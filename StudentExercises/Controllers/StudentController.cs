using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using StudentExercises.Models;

namespace StudentExercises.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly IConfiguration _config;

        public StudentController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }


        /*******************
        * Get all students
        *******************/
        [HttpGet(Name = "GetAllStudents")]
        public List<Student> GetAllStudents(string include, string q)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    if (include == "exercise" || include == "exercises")
                    {
                        cmd.CommandText = @"SELECT s.Id as StudentId, s.FirstName, s.LastName, s.Slack, s.CohortId,
                                                   c.CohortName, c.Id,  e.id as ExerciseId, e.ExerciseName, e.ExerciseLanguage
                                            FROM Student s
                                            LEFT JOIN Cohort c on s.CohortId = c.id
                                            LEFT JOIN StudentExercise se on s.id = se.studentid
                                            LEFT JOIN Exercise e on se.exerciseid = e.id
                                            WHERE 1=1";
                    }
                    else
                    {
                        cmd.CommandText = @"SELECT s.Id as StudentId, s.FirstName, s.LastName, s.Slack, s.CohortId,
                                                   c.CohortName, c.Id
                                            FROM Student s
                                            LEFT JOIN Cohort c on s.CohortId = c.id
                                            WHERE 1=1";
                    }

                    if (!string.IsNullOrWhiteSpace(q))
                    {
                        cmd.CommandText += @" AND
                                             (s.FirstName LIKE @q OR s.LastName LIKE @q OR s.Slack LIKE @q)";
                        cmd.Parameters.Add(new SqlParameter("@q", $"%{q}%"));
                    }

                    SqlDataReader reader = cmd.ExecuteReader();

                    Dictionary<int, Student> students = new Dictionary<int, Student>();
                    while (reader.Read())
                    {
                        int studentId = reader.GetInt32(reader.GetOrdinal("StudentId"));
                        if (!students.ContainsKey(studentId))
                        {
                            Student newStudent = new Student
                            {
                                Id = studentId,
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                Slack = reader.GetString(reader.GetOrdinal("Slack")),
                                CohortId = reader.GetInt32(reader.GetOrdinal("CohortId")),
                                Cohort = new Cohort
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    CohortName = reader.GetString(reader.GetOrdinal("CohortName"))
                                }
                            };

                            students.Add(studentId, newStudent);
                        }

                        if (include == "exercise" || include == "exercises")
                        {
                            if (!reader.IsDBNull(reader.GetOrdinal("ExerciseId")))
                            {
                                Student currentStudent = students[studentId];
                                currentStudent.ExerciseList.Add(
                                    new Exercise
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("ExerciseId")),
                                        ExerciseLanguage = reader.GetString(reader.GetOrdinal("ExerciseLanguage")),
                                        ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                                    }
                                );
                            }
                        }
                    }

                    reader.Close();
                    return students.Values.ToList();

                }
            }
        }

        /*******************
        * Get student
        *******************/
        [HttpGet("{id}", Name = "GetStudent")]
        public Student Get(int id, string include)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    if (include == "exercise" || include == "exercises")
                    {
                        cmd.CommandText = @"SELECT s.Id as StudentId, s.FirstName, s.LastName, s.Slack, s.CohortId,
                                                   c.CohortName, c.Id,  e.id as ExerciseId, e.ExerciseName, e.ExerciseLanguage
                                            FROM Student s
                                            LEFT JOIN Cohort c on s.CohortId = c.id
                                            LEFT JOIN StudentExercise se on s.id = se.studentid
                                            LEFT JOIN Exercise e on se.exerciseid = e.id";
                    }
                    else
                    {
                        cmd.CommandText = $@"SELECT s.Id, s.FirstName, s.LastName, s.Slack, s.CohortId, c.CohortName
                                        FROM Student s 
                                        INNER JOIN Cohort c ON s.CohortID = c.id";
                    }

                    cmd.CommandText += " WHERE s.id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();

                    Student student = null;

                    while (reader.Read())
                    {
                        if (student == null)
                        {
                            student = new Student
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                Slack = reader.GetString(reader.GetOrdinal("Slack")),
                                CohortId = reader.GetInt32(reader.GetOrdinal("CohortId")),
                                Cohort = new Cohort
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("CohortId")),
                                    CohortName = reader.GetString(reader.GetOrdinal("CohortName"))
                                }

                            };
                        }

                        if (include == "exercise" || include == "exercises")
                        {
                            if (!reader.IsDBNull(reader.GetOrdinal("ExerciseId")))
                            {
                                student.ExerciseList.Add(
                                    new Exercise
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("ExerciseId")),
                                        ExerciseLanguage = reader.GetString(reader.GetOrdinal("ExerciseLanguage")),
                                        ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                                    }
                                );
                            }
                        }
                    }
                    
                    reader.Close();
                    return student;
                }
            }
        }

        /*******************
       * Create student
       *******************/
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Student newStudent)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Student (FirstName, LastName, Slack, CohortId)
                                        OUTPUT INSERTED.Id
                                        VALUES (@firstName, @lastName, @slack, @cohortId)";
                    cmd.Parameters.Add(new SqlParameter("@firstName", newStudent.FirstName));
                    cmd.Parameters.Add(new SqlParameter("@lastName", newStudent.LastName));
                    cmd.Parameters.Add(new SqlParameter("@slack", newStudent.Slack));
                    cmd.Parameters.Add(new SqlParameter("@cohortId", newStudent.CohortId));

                    int newId = (int)cmd.ExecuteScalar(); // Expects one thing back
                    newStudent.Id = newId;
                    return CreatedAtRoute("GetStudent", new { id = newId }, newStudent);
                }
            }
        }

        /*******************
        * Edit student
        *******************/
        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Student student)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Student
                                            SET FirstName = @firstName, LastName = @lastName,
                                                Slack = @slack, CohortId = @cohortId
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@firstName", student.FirstName));
                        cmd.Parameters.Add(new SqlParameter("@lastName", student.LastName));
                        cmd.Parameters.Add(new SqlParameter("@slack", student.Slack));
                        cmd.Parameters.Add(new SqlParameter("@cohortId", student.CohortId));
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!ObjectExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }


        /*******************
        * Delete student
        *******************/
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM StudentExercise WHERE StudentId = @id;
                                            DELETE FROM Student WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!ObjectExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool ObjectExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, FirstName, LastName, Slack, CohortId
                        FROM Student
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
        

    }
}