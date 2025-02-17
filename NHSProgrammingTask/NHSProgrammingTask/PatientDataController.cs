using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Npgsql;

namespace NHSProgrammingTask
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientDataController : ControllerBase
    {
        readonly static string connString = "Host=localhost;Username=postgres;Password=password;Database=patientdb";

        [HttpPost]
        public IActionResult Post([FromBody]string transcript){
            List<PatientData> patients = GetPatientsInfo(transcript);
            SavePatientsToDatabase(patients);
            return Ok(patients);
        }    

        List<PatientData> GetPatientsInfo(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript)) return [];

            transcript = NormaliseTranscript(transcript);

            string patientPattern = @"(?:Name:\s*(?<name>[A-Za-z\s]+)\s*)?(?:\b(\d{1,2})\b\s*)?(?:date\s*of\s*birth[:\s]*(?<dob>\d{1,2}(?:st|nd|rd|th)?\s+[A-Za-z]+\s+\d{4}|[0-9\s]+|[a-zA-Z]+ \d{1,2},? \d{4}))?\s*NHS Number:\s*(?<nhs>[a-zA-Z0-9]+)?";

            MatchCollection matches = Regex.Matches(transcript, patientPattern, RegexOptions.IgnoreCase);

            List<PatientData> patients = [];

            foreach (Match match in matches)
            {
                string? name = match.Groups["name"].Success ? match.Groups["name"].Value.Trim() : null;

                string nhsNumber = match.Groups["nhs"].Value;

                DateTime? dob = ExtractDOB(match.Groups["dob"].Value.Trim());

                int? age = ExtractAge(match.Groups["age"].Value, dob);

                if (!string.IsNullOrWhiteSpace(name) || age.HasValue || dob.HasValue || !string.IsNullOrWhiteSpace(nhsNumber))
                {
                    patients.Add(new PatientData
                    {
                        Name = name,
                        Age = age,
                        DOB = dob,
                        NHSNumber = nhsNumber,
                    });
                }
            }
            return patients;
        }

        static string NormaliseTranscript(string transcript)
        {
            string unlabelledDOBPattern = @"(\d{1,2}(?:st|nd|rd|th)? of \w+ \d{4})";
            string replacement = "date of birth $1";

            // Replace using Regex
            transcript = Regex.Replace(transcript, unlabelledDOBPattern, replacement);

            transcript = Regex.Replace(transcript, @"\bname\b|\bnhs\snumber\b|\bdate\sof\sbirth\b|new-line", m => m.Value.ToLowerInvariant(), RegexOptions.IgnoreCase);

            transcript = Regex.Replace(transcript, @"\s+", " ").Trim();


            return transcript;

        }

        static DateTime? ExtractDOB(string dobText)
        {
            if (string.IsNullOrWhiteSpace(dobText)) return null;

            // Handle "03 01 01" type formats
            if (Regex.IsMatch(dobText, @"\b\d{2} \d{2} \d{2}\b"))
            {
                var parts = dobText.Split(' ');
                int day = int.Parse(parts[0]);
                int month = int.Parse(parts[1]);
                int year = int.Parse(parts[2]) + 2000; // Assume if only two digits
                return new DateTime(year, month, day);
            }

            // Handle "2nd of May 1986" type formats
            dobText = Regex.Replace(dobText, @"(st|nd|rd|th)", ""); // Remove suffixes
            if (DateTime.TryParse(dobText, out DateTime dob))
                return dob;

            return null;
        }
        static int? ExtractAge(string age, DateTime? dob)
        {
            if (int.TryParse(age, out int extractedAge)) return extractedAge;
            else return CalculateAge(dob);
        }

        static int? CalculateAge(DateTime? dob)
        {
            if (dob != null)
            {
                var today = DateTime.Today;

                var age = today.Year - dob.Value.Year;

                if (dob.Value.Date > today.AddYears(-age)) age--;

                return age;
            }
            else
            {
                return null;
            }
        }

        static void SavePatientsToDatabase(List<PatientData> patients)
        {
            
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                foreach (var patient in patients)
                {
                    string query = "INSERT INTO patients (name, age, dob, nhs_number) VALUES (@name, @age, @dob, @nhs_number)";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("name", (object)patient.Name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("age", (object)patient.Age ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("dob", (object)patient.DOB ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("nhs_number", (object)patient.NHSNumber ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }
            }

            Console.WriteLine("Patients data saved to the database.");
        }

    }
}