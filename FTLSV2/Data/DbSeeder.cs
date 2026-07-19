using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FTLSV2.Models;

namespace FTLSV2.Data
{
    public static class DbSeeder
    {
        public static void Seed(FtlsDbContext context)
        {
            try
            {
                // 1. Alter curriculum table to add curriculum_year column if it doesn't exist
                context.Database.ExecuteSqlRaw("ALTER TABLE curriculum ADD COLUMN IF NOT EXISTS curriculum_year VARCHAR(50) DEFAULT '2023';");

                // Correct department for ES 2, ES 2A, ES 7, ES 8, ES 12
                context.Database.ExecuteSqlRaw("UPDATE subject SET department_id = 84 WHERE subject_code IN ('ES 2', 'ES 2A', 'ES 7', 'ES 8', 'ES 12');");

                // Correct department for NSTP / CWTS subjects to 88
                context.Database.ExecuteSqlRaw("UPDATE subject SET department_id = 88 WHERE subject_code LIKE 'NSTP%' OR subject_code LIKE 'CWTS%';");

                // Correct department for GE AA / GE AA* to 90 (Communication, Languages, and Literature)
                context.Database.ExecuteSqlRaw("UPDATE subject SET department_id = 90 WHERE subject_code IN ('GE AA', 'GE AA*');");

                // 2. Only seed if not already seeded.
                var hasMappings = context.Curriculums.Any(c => c.DepartmentId == 2);
                if (!hasMappings)
                {

                // Define all subjects to seed for 2023, 2025, and 2027 (excluding 2027 electives)
                var items = new List<(string code, string title, int units, string yearLevel, string semester, string curriculumYear)>
                {
                    // ==========================================
                    // 2023 CURRICULUM (B.S.Cp.E. 2018)
                    // ==========================================
                    // 1st Year 1st Sem
                    ("EM 1A1", "Engineering Calculus 1", 4, "1st Year", "1st Sem", "2023"),
                    ("NS 1A1", "Chemistry for Engineers(Lec)", 3, "1st Year", "1st Sem", "2023"),
                    ("NS 1A2", "Chemistry for Engineers(Lab)", 1, "1st Year", "1st Sem", "2023"),
                    ("CPE 1A1", "Computer Engineering as a Discipline", 1, "1st Year", "1st Sem", "2023"),
                    ("CPE 1A2", "Fundamentals in Programming", 3, "1st Year", "1st Sem", "2023"),
                    ("GE RPH", "Readings in Philippine History", 3, "1st Year", "1st Sem", "2023"),
                    ("GE MMW", "Mathematics in the Modern World", 3, "1st Year", "1st Sem", "2023"),
                    ("GE TCW", "The Contemporary World", 3, "1st Year", "1st Sem", "2023"),
                    ("PE 1", "Physical Education 1", 2, "1st Year", "1st Sem", "2023"),
                    ("NSTP 1", "Civic Welfare Training Service(CWTS) 11/Reserve Officers' Training Corps(ROTC) 11", 3, "1st Year", "1st Sem", "2023"),
                    ("ReEd 1", "Initium Fidei: An Introduction to Doing Catholic Theology", 3, "1st Year", "1st Sem", "2023"),
                    ("GUIDANCE 1", "Adjustment to College Life Phase 1", 1, "1st Year", "1st Sem", "2023"),

                    // 1st Year 2nd Sem
                    ("EM 1B1", "Engineering Calculus 2 *", 4, "1st Year", "2nd Sem", "2023"),
                    ("NS 1B1", "Physics for Engineers(Lec) *", 3, "1st Year", "2nd Sem", "2023"),
                    ("NS 1B2", "Physics for Engineers(Lab) *", 1, "1st Year", "2nd Sem", "2023"),
                    ("CPE 1B1", "Programming Logic and Design", 3, "1st Year", "2nd Sem", "2023"),
                    ("KOMFIL", "Kontekstwalisadong Komunikasyon sa Filipino", 3, "1st Year", "2nd Sem", "2023"),
                    ("LIT 1", "Literatures of the Philippines", 3, "1st Year", "2nd Sem", "2023"),
                    ("EP 1", "English Proficiency Level 1", 3, "1st Year", "2nd Sem", "2023"),
                    ("PE 2", "Physical Education 2", 2, "1st Year", "2nd Sem", "2023"),
                    ("NSTP 2", "Civic Welfare Training Service(CWTS) 12/Reserve Officers' Training Corps(ROTC) 12", 3, "1st Year", "2nd Sem", "2023"),
                    ("ReEd 2", "Written That You May Believe: An Introduction to Biblical Exegesis", 3, "1st Year", "2nd Sem", "2023"),
                    ("GUIDANCE 2", "Adjustment to College Life Phase 2", 1, "1st Year", "2nd Sem", "2023"),

                    // 2nd Year 1st Sem
                    ("EM 2A1", "Differential Equations", 3, "2nd Year", "1st Sem", "2023"),
                    ("EDA 1", "Engineering Data Analysis", 3, "2nd Year", "1st Sem", "2023"),
                    ("CPE 2A1", "Object-Oriented Programming", 3, "2nd Year", "1st Sem", "2023"),
                    ("CPE 2A3", "Discrete Mathematics", 3, "2nd Year", "1st Sem", "2023"),
                    ("ES 6", "Engineering Economics", 3, "2nd Year", "1st Sem", "2023"),
                    ("AC 2A1", "Fundamentals of Electrical Circuits(Lec)", 3, "2nd Year", "1st Sem", "2023"),
                    ("AC 2A2", "Fundamentals of Electrical Circuits(Lab)", 1, "2nd Year", "1st Sem", "2023"),
                    ("GE STS", "Science, Technology, and Society", 3, "2nd Year", "1st Sem", "2023"),
                    ("ES 2A", "Computer-Aided Drafting", 2, "2nd Year", "1st Sem", "2023"),
                    ("PE 3", "Physical Education 3", 2, "2nd Year", "1st Sem", "2023"),
                    ("ReEd 3", "Our Restless Hearts: An Introduction to Doing Catholic Morality", 3, "2nd Year", "1st Sem", "2023"),

                    // 2nd Year 2nd Sem
                    ("CPE 2B1", "Advanced Engineering Mathematics for Computer Engineering", 3, "2nd Year", "2nd Sem", "2023"),
                    ("CPE 2B3", "Data Structures and Algorithms", 3, "2nd Year", "2nd Sem", "2023"),
                    ("GE PC", "Purposive Communication", 3, "2nd Year", "2nd Sem", "2023"),
                    ("AC 2B1", "Fundamentals of Electronic Circuits(Lec)", 3, "2nd Year", "2nd Sem", "2023"),
                    ("AC 2B2", "Fundamentals of Electronic Circuits(Lab)", 1, "2nd Year", "2nd Sem", "2023"),
                    ("ES 8", "Engineering Mechanics", 3, "2nd Year", "2nd Sem", "2023"),
                    ("RIZAL", "Life and Works of Dr. Jose Rizal", 3, "2nd Year", "2nd Sem", "2023"),
                    ("PE 4", "Physical Education 4", 2, "2nd Year", "2nd Sem", "2023"),
                    ("ReEd 4", "A Call to Action: An Introduction to Catholic Social Thought", 3, "2nd Year", "2nd Sem", "2023"),
                    ("FILDIS", "Filipino sa Iba't Ibang Disiplina", 3, "2nd Year", "2nd Sem", "2023"),

                    // 2nd Year Summer
                    ("CPE 2S1", "Software Design", 4, "2nd Year", "Summer", "2023"),
                    ("CPE 2S2", "Data and Digital Communications", 3, "2nd Year", "Summer", "2023"),
                    ("CPE 2S4", "Computer Engineering Drafting and Design", 2, "2nd Year", "Summer", "2023"),

                    // 3rd Year 1st Sem
                    ("CPE 3A1", "Numerical Methods", 3, "3rd Year", "1st Sem", "2023"),
                    ("CPE 3A3", "Logic Circuits and Design(Lec)", 3, "3rd Year", "1st Sem", "2023"),
                    ("CPE 3A4", "Logic Circuits and Design(Lab)", 1, "3rd Year", "1st Sem", "2023"),
                    ("ES 7", "Engineering Management", 3, "3rd Year", "1st Sem", "2023"),
                    ("CPE 3A5", "Computer Networks and Security(Lec)", 3, "3rd Year", "1st Sem", "2023"),
                    ("CPE 3A6", "Computer Networks and Security(Lab)", 1, "3rd Year", "1st Sem", "2023"),
                    ("AC 3A3", "Fundamentals of Mixed Signals and Sensors", 3, "3rd Year", "1st Sem", "2023"),
                    ("EDA 2", "Advanced Data Statistics", 3, "3rd Year", "1st Sem", "2023"),
                    ("CPE COG1", "Cognate / Track Course 1", 3, "3rd Year", "1st Sem", "2023"),
                    ("GE ET", "Ethics", 3, "3rd Year", "1st Sem", "2023"),

                    // 3rd Year 2nd Sem
                    ("CPE 3B1", "Basic Occupational Health and Safety", 3, "3rd Year", "2nd Sem", "2023"),
                    ("CPE 3B2", "Project Management for Computer Engineers", 3, "3rd Year", "2nd Sem", "2023"),
                    ("CPE 3B3", "Microprocessors (Lec)", 3, "3rd Year", "2nd Sem", "2023"),
                    ("CPE 3B4", "Microprocessors (Lab)", 1, "3rd Year", "2nd Sem", "2023"),
                    ("CPE 3B5", "Methods of Research", 3, "3rd Year", "2nd Sem", "2023"),
                    ("CPE 3B7", "CpE Laws and Professional Practice", 3, "3rd Year", "2nd Sem", "2023"),
                    ("CPE COG2", "Cognate / Track Course 2", 3, "3rd Year", "2nd Sem", "2023"),
                    ("GE UTS", "Understanding the Self", 3, "3rd Year", "2nd Sem", "2023"),
                    ("AC 3B1", "Feedback and Control Systems", 3, "3rd Year", "2nd Sem", "2023"),

                    // 3rd Year Summer
                    ("CPE 3S1", "Introduction to HDL", 2, "3rd Year", "Summer", "2023"),
                    ("CPE 3S3", "Embedded Systems(Lec)", 3, "3rd Year", "Summer", "2023"),
                    ("CPE 3S4", "Embedded Systems(Lab)", 1, "3rd Year", "Summer", "2023"),
                    ("CPE 3S5", "Database Management Systems", 3, "3rd Year", "Summer", "2023"),

                    // 4th Year 1st Sem
                    ("CPE 4A1", "Computer Architecture and Organization (Lec)", 3, "4th Year", "1st Sem", "2023"),
                    ("CPE 4A2", "Computer Architecture and Organization (Lab)", 1, "4th Year", "1st Sem", "2023"),
                    ("CPE 4A3", "Emerging Technologies in CpE", 3, "4th Year", "1st Sem", "2023"),
                    ("CPE 4A5", "CpE Practice and Design 1", 2, "4th Year", "1st Sem", "2023"),
                    ("CPE 4A7", "Digital Signal Processing(Lec)", 3, "4th Year", "1st Sem", "2023"),
                    ("CPE 4A8", "Digital Signal Processing(Lab)", 1, "4th Year", "1st Sem", "2023"),
                    ("CPE 4A9", "Operating Systems", 3, "4th Year", "1st Sem", "2023"),
                    ("ES 12", "Technopreneurship 101", 3, "4th Year", "1st Sem", "2023"),
                    ("GE AA", "Art Appreciation", 3, "4th Year", "1st Sem", "2023"),

                    // 4th Year 2nd Sem
                    ("CPE 4B1", "CpE Practice and Design 2", 3, "4th Year", "2nd Sem", "2023"),
                    ("CPE COG3", "Cognate / Track Course 3", 3, "4th Year", "2nd Sem", "2023"),
                    ("CPE 4B3", "CpE On The Job Training (240 hours)", 3, "4th Year", "2nd Sem", "2023"),
                    ("CPE 4B4", "Seminars and Field Trips", 1, "4th Year", "2nd Sem", "2023"),


                    // ==========================================
                    // 2025 CURRICULUM (B.S.Cp.E. 2018 REVISED)
                    // ==========================================
                    // 1st Year 1st Sem
                    ("EM 1A1", "Engineering Calculus 1", 4, "1st Year", "1st Sem", "2025"),
                    ("NS 1A1", "Chemistry for Engineers (Lec)", 3, "1st Year", "1st Sem", "2025"),
                    ("NS 1A2", "Chemistry for Engineers (Lab)", 1, "1st Year", "1st Sem", "2025"),
                    ("CPE 1A1", "Computer Engineering as a Discipline", 1, "1st Year", "1st Sem", "2025"),
                    ("CPE 1A2", "Fundamentals in Programming", 3, "1st Year", "1st Sem", "2025"),
                    ("GE RPH", "Readings in Philippine History", 3, "1st Year", "1st Sem", "2025"),
                    ("GE MMW", "Mathematics in the Modern World", 3, "1st Year", "1st Sem", "2025"),
                    ("GE TCW", "The Contemporary World", 3, "1st Year", "1st Sem", "2025"),
                    ("PE 1", "Physical Education 1", 2, "1st Year", "1st Sem", "2025"),
                    ("CWTS 11*", "Civic Welfare Training Services 11", 3, "1st Year", "1st Sem", "2025"),
                    ("ReEd 1", "Initium Fidei: An Introduction to Doing Catholic Theology", 3, "1st Year", "1st Sem", "2025"),
                    ("Guidance 1", "Adjustment to College Life Phase 1", 1, "1st Year", "1st Sem", "2025"),

                    // 1st Year 2nd Sem
                    ("EM 1B1", "Engineering Calculus 2", 4, "1st Year", "2nd Sem", "2025"),
                    ("NS 1B1", "Physics for Engineers (Lec)", 3, "1st Year", "2nd Sem", "2025"),
                    ("NS 1B2", "Physics for Engineers (Lab)", 1, "1st Year", "2nd Sem", "2025"),
                    ("CPE 1B1", "Programming Logic and Design", 3, "1st Year", "2nd Sem", "2025"),
                    ("GE STS", "Science, Technology and Society", 3, "1st Year", "2nd Sem", "2025"),
                    ("Lit 1", "The Literatures of the Philippines", 3, "1st Year", "2nd Sem", "2025"),
                    ("EP 1", "English Proficiency Level 1 (lec/lab)", 3, "1st Year", "2nd Sem", "2025"),
                    ("PE 2", "Physical Education 2", 2, "1st Year", "2nd Sem", "2025"),
                    ("CWTS 12*", "Civic Welfare Training Services 12", 3, "1st Year", "2nd Sem", "2025"),
                    ("ReEd 2", "Written That You May Believe: An Introduction to Biblical Exegesis", 3, "1st Year", "2nd Sem", "2025"),
                    ("Guidance 2", "Adjustment to College Life Phase 2", 1, "1st Year", "2nd Sem", "2025"),

                    // 2nd Year 1st Sem
                    ("ES 2A", "Computer-Aided Drafting", 2, "2nd Year", "1st Sem", "2025"),
                    ("EM 2A1", "Differential Equations", 3, "2nd Year", "1st Sem", "2025"),
                    ("EDA 1", "Engineering Data Analysis", 3, "2nd Year", "1st Sem", "2025"),
                    ("CPE 2A1", "Object-Oriented Programming", 3, "2nd Year", "1st Sem", "2025"),
                    ("CPE 2A3", "Discrete Mathematics", 3, "2nd Year", "1st Sem", "2025"),
                    ("ES 6", "Engineering Economics", 3, "2nd Year", "1st Sem", "2025"),
                    ("AC 2A1", "Fundamentals of Electrical Circuits", 3, "2nd Year", "1st Sem", "2025"),
                    ("AC 2A2", "Fundamentals of Electrical Circuits", 1, "2nd Year", "1st Sem", "2025"),
                    ("GE PC", "Purposive Communication", 3, "2nd Year", "1st Sem", "2025"),
                    ("PE 3", "Physical Education 3", 2, "2nd Year", "1st Sem", "2025"),
                    ("ReEd 3", "Our Restless Hearts: An Introduction to Doing Catholic Morality", 3, "2nd Year", "1st Sem", "2025"),

                    // 2nd Year 2nd Sem
                    ("CPE 2B1", "Advanced Engineering Mathematics for Computer Engineering", 3, "2nd Year", "2nd Sem", "2025"),
                    ("CPE 2B3", "Data Structures and Algorithms", 3, "2nd Year", "2nd Sem", "2025"),
                    ("GE AA", "Art Appreciation", 3, "2nd Year", "2nd Sem", "2025"),
                    ("AC 2B1", "Fundamentals of Electronic Circuits (Lec)", 3, "2nd Year", "2nd Sem", "2025"),
                    ("AC 2B2", "Fundamentals of Electronic Circuits (Lab)", 1, "2nd Year", "2nd Sem", "2025"),
                    ("ES 8", "Engineering Mechanics", 3, "2nd Year", "2nd Sem", "2025"),
                    ("Rizal", "Life and Works of Dr. Jose Rizal", 3, "2nd Year", "2nd Sem", "2025"),
                    ("PE 4", "Physical Education 4", 2, "2nd Year", "2nd Sem", "2025"),
                    ("ReEd 4", "A Call to Action: An Introduction to Catholic Social Thought", 3, "2nd Year", "2nd Sem", "2025"),
                    ("EfCom", "Effective Communication & Human Relations", 3, "2nd Year", "2nd Sem", "2025"),

                    // 2nd Year Summer
                    ("CPE 2S1", "Software Design", 4, "2nd Year", "Summer", "2025"),
                    ("CPE 2S2", "Data and Digital Communications", 3, "2nd Year", "Summer", "2025"),
                    ("CPE 2S4", "Computer Engineering Drafting and Design", 2, "2nd Year", "Summer", "2025"),

                    // 3rd Year 1st Sem
                    ("CPE 3A1", "Numerical Methods", 3, "3rd Year", "1st Sem", "2025"),
                    ("CPE 3A3", "Logic Circuits and Design (Lec)", 3, "3rd Year", "1st Sem", "2025"),
                    ("CPE 3A4", "Logic Circuits and Designs (Lab)", 1, "3rd Year", "1st Sem", "2025"),
                    ("ES 7", "Engineering Management", 3, "3rd Year", "1st Sem", "2025"),
                    ("CPE 3A5", "Computer Networks and Security (Lec)", 3, "3rd Year", "1st Sem", "2025"),
                    ("CPE 3A6", "Computer Networks and Security (Lab)", 1, "3rd Year", "1st Sem", "2025"),
                    ("AC 3A3", "Fundamentals of Mixed Signals and Sensors", 3, "3rd Year", "1st Sem", "2025"),
                    ("EDA 2", "Advanced Engineering Data Analysis", 3, "3rd Year", "1st Sem", "2025"),
                    ("CPE COG1", "Cognate/Elective Course 1", 3, "3rd Year", "1st Sem", "2025"),
                    ("GE ET", "Ethics", 3, "3rd Year", "1st Sem", "2025"),

                    // 3rd Year 2nd Sem
                    ("CPE 3B1", "Basic Occupational Health and Safety", 3, "3rd Year", "2nd Sem", "2025"),
                    ("CPE 3B2", "Project Management for Computer Engineers", 3, "3rd Year", "2nd Sem", "2025"),
                    ("CPE 3B3", "Microprocessors (Lec)", 3, "3rd Year", "2nd Sem", "2025"),
                    ("CPE 3B4", "Micrprocessors (Lab)", 1, "3rd Year", "2nd Sem", "2025"),
                    ("CPE 3B5", "Methods of Research", 3, "3rd Year", "2nd Sem", "2025"),
                    ("CPE 3B7", "CpE Laws and Professional Practice", 3, "3rd Year", "2nd Sem", "2025"),
                    ("CPE COG2", "Cognate/Elective Course 2", 3, "3rd Year", "2nd Sem", "2025"),
                    ("GE UTS", "Understanding the Self", 3, "3rd Year", "2nd Sem", "2025"),
                    ("AC 3B1", "Feedback and Control Systems", 3, "3rd Year", "2nd Sem", "2025"),

                    // 3rd Year Summer
                    ("CPE 3S1", "Introduction to HDL", 2, "3rd Year", "Summer", "2025"),
                    ("CPE 3S3", "Embedded Systems (Lec)", 3, "3rd Year", "Summer", "2025"),
                    ("CPE 3S4", "Embedded Systems (Lab)", 1, "3rd Year", "Summer", "2025"),
                    ("CPE 3S5", "Database Management Systems", 3, "3rd Year", "Summer", "2025"),

                    // 4th Year 1st Sem
                    ("CPE 4A1", "Computer Architecture and Organization (Lec)", 3, "4th Year", "1st Sem", "2025"),
                    ("CPE 4A2", "Computer Architecture and Organization (Lab)", 1, "4th Year", "1st Sem", "2025"),
                    ("CPE 4A3", "Emerging Technologies in CpE", 3, "4th Year", "1st Sem", "2025"),
                    ("CPE 4A5", "CpE Practice and Design 1", 2, "4th Year", "1st Sem", "2025"),
                    ("CPE 4A7", "Digital Signal Processing (Lec)", 3, "4th Year", "1st Sem", "2025"),
                    ("CPE 4A8", "Digital Signal Processing (Lab)", 1, "4th Year", "1st Sem", "2025"),
                    ("CPE 4A9", "Operating Systems", 3, "4th Year", "1st Sem", "2025"),
                    ("ES 12", "Technopreneurship 101", 3, "4th Year", "1st Sem", "2025"),
                    ("GE EPM", "Eastern Philosophy", 3, "4th Year", "1st Sem", "2025"),

                    // 4th Year 2nd Sem
                    ("CPE 4B1", "CpE Practice and Design", 3, "4th Year", "2nd Sem", "2025"),
                    ("CPE COG3", "Cognate/Elective Course 3", 3, "4th Year", "2nd Sem", "2025"),
                    ("CPE 4B3", "CpE on the Job Training (240 Hours Minimum)", 3, "4th Year", "2nd Sem", "2025"),
                    ("CPE 4B4", "Seminars and Field Trips", 1, "4th Year", "2nd Sem", "2025"),


                    // ==========================================
                    // 2027 CURRICULUM (B.S.Cp.E. 2022) — ELECTIVES EXCLUDED
                    // ==========================================
                    // 1st Year 1st Sem
                    ("EM 1A1", "Engineering Calculus 1", 3, "1st Year", "1st Sem", "2027"),
                    ("NS 1A1", "Chemistry for Engineers (Lec)", 3, "1st Year", "1st Sem", "2027"),
                    ("NS 1A2", "Chemistry for Engineers (Lab)", 1, "1st Year", "1st Sem", "2027"),
                    ("CPE 1A1", "Computer Engineering as a Discipline", 1, "1st Year", "1st Sem", "2027"),
                    ("CPE 1A2", "Fundamentals in Programming (Lec/Lab)", 2, "1st Year", "1st Sem", "2027"),
                    ("GE RPH", "Readings in Philippine History", 3, "1st Year", "1st Sem", "2027"),
                    ("GE MMW", "Mathematics in the Modern World", 3, "1st Year", "1st Sem", "2027"),
                    ("ReEd 1", "Initium Fidei: An Introduction to Doing Catholic Theology", 3, "1st Year", "1st Sem", "2027"),
                    ("PATHFit 1", "Movement Competency Training", 2, "1st Year", "1st Sem", "2027"),
                    ("CWTS 11*", "Civic Welfare Training Services 11", 3, "1st Year", "1st Sem", "2027"),
                    ("GUIDANCE 1", "Adjustment to Josenian College Life", 1, "1st Year", "1st Sem", "2027"),

                    // 1st Year 2nd Sem
                    ("EM 1B1", "Engineering Calculus 2", 3, "1st Year", "2nd Sem", "2027"),
                    ("NS 1B1", "Physics for Engineers (Lec)", 3, "1st Year", "2nd Sem", "2027"),
                    ("NS 1B2", "Physics for Engineers (Lab)", 1, "1st Year", "2nd Sem", "2027"),
                    ("CPE 1B1", "Programming Logic and Design", 3, "1st Year", "2nd Sem", "2027"),
                    ("GE STS", "Science, Technology and Society", 3, "1st Year", "2nd Sem", "2027"),
                    ("EP 1", "English Proficiency Level 1 (lec/lab)", 3, "1st Year", "2nd Sem", "2027"),
                    ("ReEd 2", "Written That You May Believe: An Introduction to Biblical Exegesis", 3, "1st Year", "2nd Sem", "2027"),
                    ("PATHFit 2", "Exercise-Based Fitness Activities", 2, "1st Year", "2nd Sem", "2027"),
                    ("CWTS 12*", "Civic Welfare Training Services 12", 3, "1st Year", "2nd Sem", "2027"),
                    ("GUIDANCE 2", "Living the Augustinian Recollect College Experience", 1, "1st Year", "2nd Sem", "2027"),

                    // 2nd Year 1st Sem
                    ("EM 2A1", "Differential Equations", 3, "2nd Year", "1st Sem", "2027"),
                    ("EDA 1", "Engineering Data Analysis", 3, "2nd Year", "1st Sem", "2027"),
                    ("CPE 2A1", "Object-Oriented Programming", 3, "2nd Year", "1st Sem", "2027"),
                    ("CPE 2A3", "Discrete Mathematics", 3, "2nd Year", "1st Sem", "2027"),
                    ("ES 2", "Computer Aided-Drafting", 1, "2nd Year", "1st Sem", "2027"),
                    ("ES 6", "Engineering Economics", 3, "2nd Year", "1st Sem", "2027"),
                    ("AC 2A1", "Fundamentals of Electrical Circuits", 3, "2nd Year", "1st Sem", "2027"),
                    ("AC 2A2", "Fundamentals of Electrical Circuits", 1, "2nd Year", "1st Sem", "2027"),
                    ("GE TCW", "The Contemporary World", 3, "2nd Year", "1st Sem", "2027"),
                    ("ReEd 3", "Our Restless Hearts: An Introduction to Doing Catholic Morality", 3, "2nd Year", "1st Sem", "2027"),
                    ("PATHFit 3", "Menu of Dance, Sports, Martial Arts, Group Exercise, Outdoor and Adventure Activities 1", 2, "2nd Year", "1st Sem", "2027"),

                    // 2nd Year 2nd Sem
                    ("CPE 2B3", "Data Structures and Algorithms", 3, "2nd Year", "2nd Sem", "2027"),
                    ("AC 2B1", "Fundamentals of Electronic Circuits (Lec)", 3, "2nd Year", "2nd Sem", "2027"),
                    ("AC 2B2", "Fundamentals of Electronic Circuits (Lab)", 1, "2nd Year", "2nd Sem", "2027"),
                    ("ES 14", "Environmental Science and Engineering", 3, "2nd Year", "2nd Sem", "2027"),
                    ("GE AA", "Art Appreciation", 3, "2nd Year", "2nd Sem", "2027"),
                    ("GE PC", "Purposive Communication", 3, "2nd Year", "2nd Sem", "2027"),
                    ("GE UTS", "Understanding the Self", 3, "2nd Year", "2nd Sem", "2027"),
                    ("Rizal", "Life and Works of Dr. Jose Rizal", 3, "2nd Year", "2nd Sem", "2027"),
                    ("ReEd 4", "A Call to Action: An Introduction to Catholic Social Thought", 3, "2nd Year", "2nd Sem", "2027"),
                    ("PATHFit 4", "Menu of Dance, Sports, Martial Arts, Group Exercise, Outdoor and Adventure Activities 2", 2, "2nd Year", "2nd Sem", "2027"),

                    // 2nd Year Summer
                    ("CPE 2S1", "Software Design", 4, "2nd Year", "Summer", "2027"),
                    ("CPE 2S2", "Data and Digital Communications", 3, "2nd Year", "Summer", "2027"),
                    ("ES 7", "Engineering Management", 2, "2nd Year", "Summer", "2027"),

                    // 3rd Year 1st Sem
                    ("CPE 3A1", "Numerical Methods", 3, "3rd Year", "1st Sem", "2027"),
                    ("CPE 3A3", "Logic Circuits and Design (Lec)", 3, "3rd Year", "1st Sem", "2027"),
                    ("CPE 3A4", "Logic Circuits and Designs (Lab)", 1, "3rd Year", "1st Sem", "2027"),
                    ("CPE 3A5", "Computer Networks and Security (Lec)", 3, "3rd Year", "1st Sem", "2027"),
                    ("CPE 3A6", "Computer Networks and Security (Lab)", 1, "3rd Year", "1st Sem", "2027"),
                    ("CPE 3A8", "Computer Engineering Drafting and Design", 1, "3rd Year", "1st Sem", "2027"),
                    ("AC 3A3", "Fundamentals of Mixed Signals and Sensors", 3, "3rd Year", "1st Sem", "2027"),
                    ("GE ET", "Ethics", 3, "3rd Year", "1st Sem", "2027"),
                    ("EfCom", "Effective Communication", 3, "3rd Year", "1st Sem", "2027"),

                    // 3rd Year 2nd Sem
                    ("CPE 3B1", "Basic Occupational Health and Safety", 3, "3rd Year", "2nd Sem", "2027"),
                    ("CPE 3B2", "Project Management for Computer Engineers", 2, "3rd Year", "2nd Sem", "2027"),
                    ("CPE 3B3", "Microprocessors (Lec)", 3, "3rd Year", "2nd Sem", "2027"),
                    ("CPE 3B4", "Micrprocessors (Lab)", 1, "3rd Year", "2nd Sem", "2027"),
                    ("CPE 3B5", "Methods of Research", 3, "3rd Year", "2nd Sem", "2027"),
                    ("CPE 3B7", "Introduction to HDL", 3, "3rd Year", "2nd Sem", "2027"),
                    ("CPE COG1", "Cognate/Elective Course 1", 3, "3rd Year", "2nd Sem", "2027"),
                    ("AC 3B1", "Feedback and Control Systems", 3, "3rd Year", "2nd Sem", "2027"),
                    ("ES 12", "Technopreneurship 101", 3, "3rd Year", "2nd Sem", "2027"),

                    // 3rd Year Summer
                    ("CPE 3S3", "Embedded Systems (Lec)", 3, "3rd Year", "Summer", "2027"),
                    ("CPE 3S4", "Embedded Systems (Lab)", 1, "3rd Year", "Summer", "2027"),
                    ("CPE 3S5", "Database Management Systems", 3, "3rd Year", "Summer", "2027"),
                    ("CPE 3S7", "CpE Laws and Professional Practice", 2, "3rd Year", "Summer", "2027"),

                    // 4th Year 1st Sem
                    ("CPE 4A1", "Computer Architecture and Organization (Lec)", 3, "4th Year", "1st Sem", "2027"),
                    ("CPE 4A2", "Computer Architecture and Organization (Lab)", 1, "4th Year", "1st Sem", "2027"),
                    ("CPE 4A3", "Emerging Technologies in CpE", 3, "4th Year", "1st Sem", "2027"),
                    ("CPE 4A5", "CpE Practice and Design 1", 2, "4th Year", "1st Sem", "2027"),
                    ("CPE 4A7", "Digital Signal Processing (Lec)", 3, "4th Year", "1st Sem", "2027"),
                    ("CPE 4A8", "Digital Signal Processing (Lab)", 1, "4th Year", "1st Sem", "2027"),
                    ("CPE 4A9", "Operating Systems", 3, "4th Year", "1st Sem", "2027"),
                    ("CPE COG2", "Cognate/Elective Course 2", 3, "4th Year", "1st Sem", "2027"),
                    ("GE EPM", "Eastern Philosophy", 3, "4th Year", "1st Sem", "2027"),

                    // 4th Year 2nd Sem
                    ("CPE 4B1", "CpE Practice and Design", 3, "4th Year", "2nd Sem", "2027"),
                    ("CPE COG3", "Cognate/Elective Course 3", 3, "4th Year", "2nd Sem", "2027"),
                    ("CPE 4B3", "CpE on the Job Training (240 Hours Minimum)", 3, "4th Year", "2nd Sem", "2027"),
                    ("CPE 4B4", "Seminars and Field Trips", 1, "4th Year", "2nd Sem", "2027"),
                };

                foreach (var item in items)
                {
                    // Find or create the subject. Subjects are unique by Code and Units.
                    string trimmedCode = item.code.Trim();
                    var subject = context.Subjects
                        .FirstOrDefault(s => s.Code.Trim() == trimmedCode && s.Units == item.units && !s.Is_delete);

                    if (subject == null)
                    {
                        subject = new Subject
                        {
                            Code = item.code,
                            Title = item.title,
                            Units = item.units,
                            DepartmentId = GetDepartmentIdForSubject(item.code),
                            Is_delete = false
                        };
                        context.Subjects.Add(subject);
                        context.SaveChanges();
                    }
                    else
                    {
                        // Ensure existing subject has the correct department as well
                        int correctDeptId = GetDepartmentIdForSubject(item.code);
                        if (subject.DepartmentId != correctDeptId)
                        {
                            subject.DepartmentId = correctDeptId;
                        }
                    }

                    // Map to curriculum
                    var curriculum = new Curriculum
                    {
                        SubjectId = subject.SubjectId,
                        DepartmentId = 2, // Computer Engineering
                        YearLevel = item.yearLevel,
                        Semester = item.semester,
                        CurriculumYear = item.curriculumYear
                    };
                    context.Curriculums.Add(curriculum);
                }

                context.SaveChanges();
                Console.WriteLine("SUCCESS: Seeded 2023, 2025, and 2027 CpE Curriculums successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR Seeding: {ex.Message}");
            }
        }

        private static int GetDepartmentIdForSubject(string code)
        {
            string upperCode = code.Trim().ToUpper();
            if (upperCode.StartsWith("CPE") || upperCode.StartsWith("AC "))
                return 2; // Computer Engineering
            if (upperCode.StartsWith("CS"))
                return 29; // Computer Science
            if (upperCode.StartsWith("IT"))
                return 31; // Information Technology
            if (upperCode.StartsWith("EM") || upperCode.StartsWith("EDA") || 
                upperCode.StartsWith("ES 2") || upperCode.StartsWith("ES 6") || 
                upperCode.StartsWith("ES 7") || upperCode.StartsWith("ES 8") || 
                upperCode.StartsWith("ES 12") || 
                upperCode.StartsWith("NS") || upperCode.StartsWith("PHY"))
                return 84; // Engineering Mathematics, Science and Enhancement Program
            if (upperCode.StartsWith("NSTP") || upperCode.StartsWith("CWTS"))
                return 88; // National Service Training Program
            if (upperCode.StartsWith("PE ") || upperCode.StartsWith("PATHFIT"))
                return 87; // Physical Education
            if (upperCode.StartsWith("REED"))
                return 83; // Center for Religious Education
            if (upperCode.StartsWith("GUIDANCE"))
                return 89; // Student Development and Placement Center
            if (upperCode.StartsWith("KOMFIL") || upperCode.StartsWith("LIT") || upperCode.StartsWith("EP ") || upperCode.StartsWith("FILDIS") || upperCode.StartsWith("EFCOM"))
                return 90; // Communication, Languages, and Literature
            if (upperCode.StartsWith("ENT"))
                return 85; // Business and Entrepreneurship
            if (upperCode.StartsWith("GE ") || upperCode.StartsWith("RIZAL"))
            {
                if (upperCode.Contains("PC") || upperCode.Contains("AA")) // GE PC (Purposive Communication) & GE AA (Art Appreciation)
                    return 90; // Communication, Languages, and Literature
                if (upperCode.Contains("MMW")) // GE MMW (Mathematics in the Modern World)
                    return 86; // Mathematics and Sciences
                return 91; // Department of Social Sciences and Philosophy
            }
            return 2; // Default to Computer Engineering
        }
    }
}
