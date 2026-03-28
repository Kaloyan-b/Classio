using Classio.Models;
using Classio.Areas.Teacher.Models;
using Microsoft.AspNetCore.Identity;
using TeacherAttendanceState = Classio.Areas.Teacher.Models.AttendanceState;

namespace Classio.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ClassioDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            // ── Roles ─────────────────────────────────────────────────────────────
            foreach (var role in new[] { "Admin", "Student", "Parent", "Teacher" })
            {
                if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
                    roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
            }

            // ── Admin (always ensure) ─────────────────────────────────────────────
            EnsureUser(userManager, "admin@classio.com", "Admin123!", "System", "Admin", "Admin");

            // Already seeded — skip the rest
            if (!context.Teachers.Any())
            {
            var rand = new Random(42);

            // ── School ────────────────────────────────────────────────────────────
            var school = new School { Name = "Sofia High Academy" };
            context.Schools.Add(school);
            context.SaveChanges();

            // ── Subjects ──────────────────────────────────────────────────────────
            var subjects = new Subject[]
            {
                new Subject { Name = "Mathematics" },
                new Subject { Name = "Biology" },
                new Subject { Name = "History" },
                new Subject { Name = "Physics" },
            };
            context.Subjects.AddRange(subjects);
            context.SaveChanges();

            // ── Teachers ──────────────────────────────────────────────────────────
            var teacherDefs = new[]
            {
                ("Thomas",  "Anderson", "t.anderson@classio.com", subjects[0].Id),
                ("Valerie", "Frizzle",  "v.frizzle@classio.com",  subjects[1].Id),
                ("George",  "Feeny",    "g.feeny@classio.com",    subjects[2].Id),
                ("Marie",   "Curie",    "m.curie@classio.com",    subjects[3].Id),
            };

            var teachers = new List<Teacher>();
            foreach (var (first, last, email, subjectId) in teacherDefs)
            {
                var user = EnsureUser(userManager, email, "Password123!", first, last, "Teacher");
                teachers.Add(new Teacher
                {
                    FirstName = first,
                    LastName = last,
                    UserId = user.Id,
                    SubjectId = subjectId,
                    SchoolId = school.Id
                });
            }
            context.Teachers.AddRange(teachers);
            context.SaveChanges();

            // ── Classes ───────────────────────────────────────────────────────────
            var class11A = new Class { Name = "11A", SchoolId = school.Id, HeadTeacherId = teachers[0].Id };
            var class12A = new Class { Name = "12A", SchoolId = school.Id, HeadTeacherId = teachers[1].Id };
            context.Classes.AddRange(class11A, class12A);
            context.SaveChanges();

            // ── Parents ───────────────────────────────────────────────────────────
            var parentDefs = new[]
            {
                ("Mary",   "Johnson", "mary.johnson@classio.com"),
                ("Robert", "Smith",   "robert.smith@classio.com"),
            };

            var parents = new List<Parent>();
            foreach (var (first, last, email) in parentDefs)
            {
                var user = EnsureUser(userManager, email, "Password123!", first, last, "Parent");
                parents.Add(new Parent { FirstName = first, LastName = last, UserId = user.Id });
            }
            context.Parents.AddRange(parents);
            context.SaveChanges();

            // ── Students ──────────────────────────────────────────────────────────
            var studentDefs = new[]
            {
                ("Alice",   "Johnson", "alice@classio.com",   class12A.Id, (int?)parents[0].Id, (int?)null),
                ("Bob",     "Smith",   "bob@classio.com",     class12A.Id, (int?)parents[1].Id, (int?)null),
                ("Charlie", "Brown",   "charlie@classio.com", class12A.Id, (int?)null,          (int?)null),
                ("Diana",   "Prince",  "diana@classio.com",   class11A.Id, (int?)parents[0].Id, (int?)null),
                ("Ethan",   "Hunt",    "ethan@classio.com",   class11A.Id, (int?)parents[1].Id, (int?)null),
                ("Fiona",   "Green",   "fiona@classio.com",   class11A.Id, (int?)null,          (int?)null),
            };

            var students = new List<Student>();
            foreach (var (first, last, email, classId, p1Id, p2Id) in studentDefs)
            {
                var user = EnsureUser(userManager, email, "Password123!", first, last, "Student");
                students.Add(new Student
                {
                    FirstName = first,
                    LastName = last,
                    UserId = user.Id,
                    ClassId = classId,
                    SchoolId = school.Id,
                    Parent1Id = p1Id,
                    Parent2Id = p2Id
                });
            }
            context.Students.AddRange(students);
            context.SaveChanges();

            // ── Grades ────────────────────────────────────────────────────────────
            double[] gradeValues = { 3.00, 3.50, 4.00, 4.50, 5.00, 5.50, 6.00 };
            GradeType[] gradeTypes = { GradeType.VerbalExam, GradeType.Test, GradeType.Homework, GradeType.Quiz, GradeType.Project };

            var grades = new List<Grade>();
            foreach (var student in students)
            {
                foreach (var teacher in teachers)
                {
                    int count = rand.Next(4, 9);
                    for (int i = 0; i < count; i++)
                    {
                        grades.Add(new Grade
                        {
                            Value = gradeValues[rand.Next(gradeValues.Length)],
                            Type = gradeTypes[rand.Next(gradeTypes.Length)],
                            Date = DateTime.Now.AddDays(-rand.Next(1, 120)),
                            StudentId = student.Id,
                            TeacherId = teacher.Id,
                            SubjectId = teacher.SubjectId
                        });
                    }
                }
            }
            context.Grades.AddRange(grades);

            // ── Absences ──────────────────────────────────────────────────────────
            var absences = new List<Absence>();
            foreach (var student in students)
            {
                int count = rand.Next(3, 9);
                for (int i = 0; i < count; i++)
                {
                    var teacher = teachers[rand.Next(teachers.Count)];
                    absences.Add(new Absence
                    {
                        Date = DateTime.Now.AddDays(-rand.Next(1, 120)),
                        AttendanceState = rand.Next(2) == 0 ? TeacherAttendanceState.Absent : TeacherAttendanceState.Late,
                        StudentId = student.Id,
                        SubjectId = teacher.SubjectId
                    });
                }
            }
            context.Absences.AddRange(absences);
            context.SaveChanges();
            }

            // ── Class Periods (independent check) ───────────────────────────────
            if (!context.ClassPeriods.Any())
            {
                var start = new TimeSpan(8, 30, 0);
                var lessonLength = TimeSpan.FromMinutes(40);
                var shortBreak = TimeSpan.FromMinutes(10);
                var longBreak = TimeSpan.FromMinutes(30);
                int order = 1;
                int lessonNum = 0;

                var periods = new List<ClassPeriod>();
                for (int i = 0; i < 8; i++)
                {
                    if (i > 0)
                    {
                        var breakDuration = (i == 5) ? longBreak : shortBreak;
                        var breakPeriod = new ClassPeriod
                        {
                            Name = (i == 5) ? "Long Break" : "Break",
                            StartTime = start,
                            EndTime = start + breakDuration,
                            IsBreak = true,
                            Order = order++
                        };
                        periods.Add(breakPeriod);
                        start += breakDuration;
                    }

                    lessonNum++;
                    var lesson = new ClassPeriod
                    {
                        Name = $"Period {lessonNum}",
                        StartTime = start,
                        EndTime = start + lessonLength,
                        IsBreak = false,
                        Order = order++
                    };
                    periods.Add(lesson);
                    start += lessonLength;
                }
                context.ClassPeriods.AddRange(periods);
                context.SaveChanges();

                // ── Schedule Slots (Mon–Fri for each class) ─────────────────
                var lessonPeriods = periods.Where(p => !p.IsBreak).ToList();
                var allClasses = context.Classes.ToList();
                var allTeachers = context.Teachers.ToList();
                var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

                if (allTeachers.Any())
                {
                    var slots = new List<ScheduleSlot>();
                    foreach (var cls in allClasses)
                    {
                        foreach (var day in weekdays)
                        {
                            for (int i = 0; i < lessonPeriods.Count; i++)
                            {
                                var teacher = allTeachers[(i + (int)day) % allTeachers.Count];
                                slots.Add(new ScheduleSlot
                                {
                                    DayOfWeek = day,
                                    ClassId = cls.Id,
                                    ClassPeriodId = lessonPeriods[i].Id,
                                    SubjectId = teacher.SubjectId,
                                    TeacherId = teacher.Id
                                });
                            }
                        }
                    }
                    context.ScheduleSlots.AddRange(slots);
                    context.SaveChanges();
                }
            }
        }

        private static User EnsureUser(UserManager<User> userManager, string email, string password, string firstName, string lastName, string role)
        {
            var user = userManager.FindByEmailAsync(email).GetAwaiter().GetResult();
            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true
                };
                var result = userManager.CreateAsync(user, password).GetAwaiter().GetResult();
                if (!result.Succeeded)
                    throw new Exception($"Failed to create user {email}: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            if (!userManager.IsInRoleAsync(user, role).GetAwaiter().GetResult())
            {
                var result = userManager.AddToRoleAsync(user, role).GetAwaiter().GetResult();
                if (!result.Succeeded)
                    throw new Exception($"Failed to assign role '{role}' to {email}: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return user;
        }
    }
}
