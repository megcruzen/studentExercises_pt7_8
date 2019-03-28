using Newtonsoft.Json;
using StudentExercises.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace StudentExerciseAPI.Tests
{
    public class StudentTest
    {
        /************
         * GET Test
         ***********/
        [Fact]
        public async Task Test_Get_All_Students()
        {

            using (var client = new APIClientProvider().Client)
            {
                /* ARRANGE */

                /* ACT */
                var response = await client.GetAsync("/api/student");
                
                string responseBody = await response.Content.ReadAsStringAsync();
                var studentList = JsonConvert.DeserializeObject<List<Student>>(responseBody);

                /* ASSERT */
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(studentList.Count > 0);
            }
        }

        /*************
         * POST Test
         ************/
        [Fact]
        public async Task Test_Create_Student()
        {
            /*
                Generate a new instance of an HttpClient that you can
                use to generate HTTP requests to your API controllers.
                The `using` keyword will automatically dispose of this
                instance of HttpClient once your code is done executing.
            */
            using (var client = new APIClientProvider().Client)
            {
                /*  ARRANGE */

                // Construct a new student object to be sent to the API
                Student newStudent = new Student
                {
                    FirstName = "New",
                    LastName = "Student",
                    Slack = "newstudent",
                    CohortId = 1
                };

                // Serialize the C# object into a JSON string
                var studentAsJSON = JsonConvert.SerializeObject(newStudent);


                /* ACT */

                // Use the client to send the request and store the response
                var response = await client.PostAsync(
                    "/api/student",
                    new StringContent(studentAsJSON, Encoding.UTF8, "application/json")
                );

                // Store the JSON body of the response
                string responseBody = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON into an instance of Student
                var returnedStudent = JsonConvert.DeserializeObject<Student>(responseBody);


                /* ASSERT */

                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                Assert.Equal("New", returnedStudent.FirstName);
                Assert.Equal("Student", returnedStudent.LastName);
                Assert.Equal("newstudent", returnedStudent.Slack);
                Assert.Equal(1, returnedStudent.CohortId);
            }
        }


        /*************
         * PUT Test
         ************/
        [Fact]
        public async Task Test_Modify_Student()
        {
            // New cohort Id to change to and test
            string newName = "Meg";

            using (var client = new APIClientProvider().Client)
            {
                /* PUT section */
                Student modifiedStudent = new Student
                {
                    FirstName = newName,
                    LastName = "Cruzen",
                    Slack = "megcruzen",
                    CohortId = 2
                };
                var modifiedStudentAsJSON = JsonConvert.SerializeObject(modifiedStudent);

                var response = await client.PutAsync(
                    "/api/student/12",
                    new StringContent(modifiedStudentAsJSON, Encoding.UTF8, "application/json")
                );
                string responseBody = await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);


                /* GET section - Verify that the PUT operation was successful */
                var getStudent = await client.GetAsync("/api/student/12");
                getStudent.EnsureSuccessStatusCode();

                string getStudentBody = await getStudent.Content.ReadAsStringAsync();
                Student newStudent = JsonConvert.DeserializeObject<Student>(getStudentBody);

                Assert.Equal(HttpStatusCode.OK, getStudent.StatusCode);
                Assert.Equal(newName, newStudent.FirstName);
            }
        }


        /*************
         * DELETE Test
         ************/
        [Fact]
        public async Task Test_Delete_Student()
        {

            using (var client = new APIClientProvider().Client)
            {
                /* DELETE section */
                var response = await client.DeleteAsync("/api/student/2");
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);


                /* GET section - Verify that the DELETE operation was successful */
                var getStudent = await client.GetAsync("/api/student/2");
                Assert.Equal(HttpStatusCode.NoContent, getStudent.StatusCode);
            }
        }

    }
}
