using System;

namespace HospitalDomainLib
{
    public class Patient
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Diagnosis { get; set; }
        public DateTime AdmissionDate { get; set; }

        public Patient(int id, string name, int age, string diagnosis)
        {
            Id = id;
            Name = name;
            Age = age;
            Diagnosis = diagnosis;
            AdmissionDate = DateTime.Now.AddDays(-new Random().Next(0, 365));
        }
    }
}