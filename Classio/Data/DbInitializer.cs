using Classio.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Classio.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ClassioDbContext context)
        {
            context.Database.EnsureCreated();

            var roleNames = new[] { "Teacher", "Student", "Parent" };
            foreach (var roleName in roleNames)
            {
                if (!context.Roles.Any(r => r.Name == roleName))
                {
                    context.Roles.Add(new IdentityRole
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = roleName,
                        NormalizedName = roleName.ToUpper(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    });
                }
            }
            context.SaveChanges();

            var teacherRoleId = context.Roles.Single(r => r.Name == "Teacher").Id;
            var studentRoleId = context.Roles.Single(r => r.Name == "Student").Id;
            var parentRoleId = context.Roles.Single(r => r.Name == "Parent").Id;


            if (context.UserRoles.Any())
            {
                context.UserRoles.RemoveRange(context.UserRoles);
                context.SaveChanges();
            }

            var teachersIds = new[] { "u-t1", "u-t2", "u-t3" };
            foreach (var id in teachersIds)
            {
                if (context.Users.Any(u => u.Id == id))
                    context.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = teacherRoleId });
            }

            var studentsIds = new[] { "u-s1", "u-s2", "u-s3", "u-s4", "u-s5" };
            foreach (var id in studentsIds)
            {
                if (context.Users.Any(u => u.Id == id))
                    context.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = studentRoleId });
            }

            if (context.Users.Any(u => u.Id == "u-p1"))
            {
                context.UserRoles.Add(new IdentityUserRole<string> { UserId = "u-p1", RoleId = parentRoleId });
            }

            context.SaveChanges();


            var unhashedUsers = context.Users.Where(u => u.PasswordHash == null).ToList();
            if (unhashedUsers.Any())
            {
                var passwordHasher = new PasswordHasher<User>();
                foreach (var user in unhashedUsers)
                {
                    user.NormalizedUserName = user.UserName.ToUpper();
                    user.NormalizedEmail = user.Email.ToUpper();
                    user.SecurityStamp = Guid.NewGuid().ToString();
                    user.PasswordHash = passwordHasher.HashPassword(user, "Password123!");
                }
                context.SaveChanges();
            }


            if (context.Grades.Count() < 50 && context.Students.Any() && context.Teachers.Any())
            {
                var dbStudents = context.Students.ToList();
                var dbTeachers = context.Teachers.ToList();

                var rand = new Random();
                var gradeDescriptions = new[] { "Homework", "Pop Quiz", "Midterm Exam", "Final Project", "Participation", "Essay", "Lab Report", "Presentation" };
                double[] possibleGrades = { 3.00, 3.50, 4.00, 4.50, 5.00, 5.50, 6.00 }; 

                var newGrades = new List<Grade>();
                var newAbsences = new List<Absence>();

                foreach (var student in dbStudents)
                {

                    foreach (var teacher in dbTeachers)
                    {
                        int numGrades = rand.Next(5, 9);
                        for (int i = 0; i < numGrades; i++)
                        {
                            newGrades.Add(new Grade
                            {
                                Value = possibleGrades[rand.Next(possibleGrades.Length)],
                                Date = DateTime.Now.AddDays(-rand.Next(1, 100)), 
                                Description = gradeDescriptions[rand.Next(gradeDescriptions.Length)],
                                StudentId = student.Id,
                                TeacherId = teacher.Id,
                                SubjectId = teacher.SubjectId
                            });
                        }
                    }


                    int numAbsences = rand.Next(2, 7);
                    for (int i = 0; i < numAbsences; i++)
                    {
                        var randomTeacher = dbTeachers[rand.Next(dbTeachers.Count)];
                        newAbsences.Add(new Absence
                        {
                            Date = DateTime.Now.AddDays(-rand.Next(1, 100)),
                            StudentId = student.Id,
                            SubjectId = randomTeacher.SubjectId
                        });
                    }
                }

                context.Grades.AddRange(newGrades);
                context.Absences.AddRange(newAbsences);
                context.SaveChanges();
            }


            if (context.Teachers.Any()) return;

            var hasher = new PasswordHasher<User>();

            var users = new User[]
            {
                new User { Id = "u-t1", UserName = "t.anderson@classio.com", NormalizedUserName = "T.ANDERSON@CLASSIO.COM", Email = "t.anderson@classio.com", NormalizedEmail = "T.ANDERSON@CLASSIO.COM", FirstName = "Thomas", LastName = "Anderson", SecurityStamp = Guid.NewGuid().ToString() },
                new User { Id = "u-t2", UserName = "v.frizzle@classio.com", NormalizedUserName = "V.FRIZZLE@CLASSIO.COM", Email = "v.frizzle@classio.com", NormalizedEmail = "V.FRIZZLE@CLASSIO.COM", FirstName = "Valerie", LastName = "Frizzle", SecurityStamp = Guid.NewGuid().ToString() },
                new User { Id = "u-t3", UserName = "g.feeny@classio.com", NormalizedUserName = "G.FEENY@CLASSIO.COM", Email = "g.feeny@classio.com", NormalizedEmail = "G.FEENY@CLASSIO.COM", FirstName = "George", LastName = "Feeny", SecurityStamp = Guid.NewGuid().ToString() },

                new User { Id = "u-s1", UserName = "alice@classio.com", NormalizedUserName = "ALICE@CLASSIO.COM", Email = "alice@classio.com", NormalizedEmail = "ALICE@CLASSIO.COM", FirstName = "Alice", LastName = "Johnson", SecurityStamp = Guid.NewGuid().ToString() },
                new User { Id = "u-s2", UserName = "bob@classio.com", NormalizedUserName = "BOB@CLASSIO.COM", Email = "bob@classio.com", NormalizedEmail = "BOB@CLASSIO.COM", FirstName = "Bob", LastName = "Smith", SecurityStamp = Guid.NewGuid().ToString() },
                new User { Id = "u-s3", UserName = "charlie@classio.com", NormalizedUserName = "CHARLIE@CLASSIO.COM", Email = "charlie@classio.com", NormalizedEmail = "CHARLIE@CLASSIO.COM", FirstName = "Charlie", LastName = "Brown", SecurityStamp = Guid.NewGuid().ToString() },
                new User { Id = "u-s4", UserName = "diana@classio.com", NormalizedUserName = "DIANA@CLASSIO.COM", Email = "diana@classio.com", NormalizedEmail = "DIANA@CLASSIO.COM", FirstName = "Diana", LastName = "Prince", SecurityStamp = Guid.NewGuid().ToString() },
                new User { Id = "u-s5", UserName = "ethan@classio.com", NormalizedUserName = "ETHAN@CLASSIO.COM", Email = "ethan@classio.com", NormalizedEmail = "ETHAN@CLASSIO.COM", FirstName = "Ethan", LastName = "Hunt", SecurityStamp = Guid.NewGuid().ToString() },

                new User { Id = "u-p1", UserName = "parent@classio.com", NormalizedUserName = "PARENT@CLASSIO.COM", Email = "parent@classio.com", NormalizedEmail = "PARENT@CLASSIO.COM", FirstName = "Mary", LastName = "Johnson", SecurityStamp = Guid.NewGuid().ToString() }
            };

            foreach (var user in users) { user.PasswordHash = hasher.HashPassword(user, "Password123!"); }
            context.Users.AddRange(users);
            context.SaveChanges();

            var school = new School { Name = "Sofia High Academy" };
            context.Schools.Add(school);
            context.SaveChanges();

            var subjects = new Subject[]
            {
                new Subject { Name = "Mathematics" },
                new Subject { Name = "Biology" },
                new Subject { Name = "History" }
            };
            context.Subjects.AddRange(subjects);
            context.SaveChanges();

            var initTeachers = new Teacher[]
            {
                new Teacher { FirstName = "Thomas", LastName = "Anderson", UserId = "u-t1", SchoolId = school.Id, SubjectId = subjects[0].Id },
                new Teacher { FirstName = "Valerie", LastName = "Frizzle", UserId = "u-t2", SchoolId = school.Id, SubjectId = subjects[1].Id },
                new Teacher { FirstName = "George", LastName = "Feeny", UserId = "u-t3", SchoolId = school.Id, SubjectId = subjects[2].Id }
            };
            context.Teachers.AddRange(initTeachers);
            context.SaveChanges();

            var class12A = new Class { Name = "12A", SchoolId = school.Id, HeadTeacherId = initTeachers[0].Id };
            context.Classes.Add(class12A);
            context.SaveChanges();

            var parent = new Parent { FirstName = "Mary", LastName = "Johnson", UserId = "u-p1" };
            context.Parents.Add(parent);
            context.SaveChanges();

            var initStudents = new Student[]
            {
                new Student { FirstName = "Alice", LastName = "Johnson", UserId = "u-s1", SchoolId = school.Id, ClassId = class12A.Id, Parent1Id = parent.Id },
                new Student { FirstName = "Bob", LastName = "Smith", UserId = "u-s2", SchoolId = school.Id, ClassId = class12A.Id },
                new Student { FirstName = "Charlie", LastName = "Brown", UserId = "u-s3", SchoolId = school.Id, ClassId = class12A.Id },
                new Student { FirstName = "Diana", LastName = "Prince", UserId = "u-s4", SchoolId = school.Id, ClassId = class12A.Id },
                new Student { FirstName = "Ethan", LastName = "Hunt", UserId = "u-s5", SchoolId = school.Id, ClassId = class12A.Id }
            };
            context.Students.AddRange(initStudents);
            context.SaveChanges();

        }
    }
}