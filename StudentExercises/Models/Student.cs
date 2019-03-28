﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace StudentExercises.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [StringLength(12, MinimumLength = 3)]
        public string Slack { get; set; }

        [Required]
        public int CohortId { get; set; }

        public Cohort Cohort { get; set; }
        public List<Exercise> ExerciseList {get; set;} = new List<Exercise>();
    }
}