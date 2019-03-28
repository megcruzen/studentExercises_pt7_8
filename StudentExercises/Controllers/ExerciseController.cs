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
    public class ExerciseController : ControllerBase
    {
        private readonly IConfiguration _config;

        public ExerciseController(IConfiguration config)
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
        * Get all exercises
        *******************/
        [HttpGet]
        public List<Exercise> Get(string include, string q)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    if (include == "students" || include == "student")
                    {
                        cmd.CommandText = @"SELECT e.Id, e.ExerciseName, e.ExerciseLanguage, s.Id AS StudId, s.FirstName, s.LastName
                                        FROM Exercise e
                                        LEFT JOIN StudentExercise se ON se.ExerciseId = e.Id
                                        LEFT JOIN Student s ON s.Id = se.StudentId
                                        WHERE 1=1";
                    }
                    else
                    {
                        cmd.CommandText = @"SELECT Id, ExerciseName, ExerciseLanguage FROM Exercise
                                            WHERE 1=1";
                    }

                    if (q != null)
                    {
                        cmd.CommandText += $@" AND 
                                              (ExerciseName LIKE @q OR ExerciseLanguage LIKE @q)";
                        cmd.Parameters.Add(new SqlParameter("@q", $"%{q}%"));
                    }

                    SqlDataReader reader = cmd.ExecuteReader();

                    Dictionary<int, Exercise> exercises = new Dictionary<int, Exercise>();
                    while (reader.Read())
                    {
                        int exerciseId = reader.GetInt32(reader.GetOrdinal("Id"));
                        if (!exercises.ContainsKey(exerciseId))
                        {
                            Exercise newExercise = new Exercise
                            {
                                Id = exerciseId,
                                ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                                ExerciseLanguage = reader.GetString(reader.GetOrdinal("ExerciseLanguage"))
                            };

                            exercises.Add(exerciseId, newExercise);
                        }

                        Exercise currentExercise = exercises[exerciseId];

                        if (include == "students" || include == "student")
                        {
                            int studentId = reader.GetInt32(reader.GetOrdinal("StudId"));
                            if (!currentExercise.StudentList.Exists(s => s.Id == studentId))
                            {
                                currentExercise.StudentList.Add(
                                new Student
                                {
                                    Id = studentId,
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("LastName"))
                                }
                                );
                            }
                        }
                    }

                    reader.Close();
                    return exercises.Values.ToList();
                }
            }
        }
        

        /*******************
        * Get one exercise
        *******************/
        [HttpGet("{id}", Name = "GetExercise")]
        public Exercise Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, ExerciseName, ExerciseLanguage
                                        FROM Exercise
                                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Exercise exercise = null;

                    if (reader.Read())
                    {
                        exercise = new Exercise
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            ExerciseName = reader.GetString(reader.GetOrdinal("ExerciseName")),
                            ExerciseLanguage = reader.GetString(reader.GetOrdinal("ExerciseLanguage"))
                        };
                    }
                    reader.Close();
                    return exercise;
                }
            }
        }

        /*******************
        * Create exercise
        *******************/
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Exercise newExercise)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Exercise (ExerciseName, ExerciseLanguage)
                                        OUTPUT INSERTED.Id
                                        VALUES (@exerciseName, @exerciseLanguage)";
                    cmd.Parameters.Add(new SqlParameter("@exerciseName", newExercise.ExerciseName));
                    cmd.Parameters.Add(new SqlParameter("@exerciseLanguage", newExercise.ExerciseLanguage));

                    int newId = (int)cmd.ExecuteScalar(); // Expects one thing back
                    newExercise.Id = newId;
                    return CreatedAtRoute("GetExercise", new { id = newId }, newExercise);
                }
            }
        }


        /*******************
        * Edit exercise
        *******************/
        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Exercise exercise)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Exercise
                                            SET ExerciseName = @exerciseName,
                                                ExerciseLanguage = @exerciseLanguage
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@exerciseName", exercise.ExerciseName));
                        cmd.Parameters.Add(new SqlParameter("@exerciseLanguage", exercise.ExerciseLanguage));
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
        * Delete exercise
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
                        cmd.CommandText = @"DELETE FROM Exercise WHERE Id = @id";
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
                        SELECT Id, ExerciseName, ExerciseLanguage
                        FROM Exercise
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }


    }
}