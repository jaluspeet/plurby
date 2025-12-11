using Plurby.Services;
using Plurby.Services.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plurby.Infrastructure
{
    public class DataGenerator
    {
        public static void InitializeUsers(PlurbyDbContext context)
        {
            if (context.Users.Any())
            {
                return;   // Data was already seeded
            }

            var users = new List<User>
            {
                new User
                {
                    Id = Guid.Parse("3de6883f-9a0b-4667-aa53-0fbc52c4d300"), // Forced to specific Guid for tests
                    Email = "beppe.vessicchio@test.it",
                    Password = "M0Cuk9OsrcS/rTLGf5SY6DUPqU2rGc1wwV2IL88GVGo=", // SHA-256 of text "Prova"
                    FirstName = "Beppe",
                    LastName = "Vessicchio",
                    NickName = "Maestro",
                    Role = UserRole.Manager
                },
                new User
                {
                    Id = Guid.Parse("a030ee81-31c7-47d0-9309-408cb5ac0ac7"), // Forced to specific Guid for tests
                    Email = "pierpaolo.pasolini@test.it",
                    Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                    FirstName = "Pierpaolo",
                    LastName = "Pasolini",
                    NickName = "PPP",
                    Role = UserRole.Employee
                },
                new User
                {
                    Id = Guid.Parse("bfdef48b-c7ea-4227-8333-c635af267354"), // Forced to specific Guid for tests
                    Email = "luciano.berio@test.it",
                    Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                    FirstName = "Luciano",
                    LastName = "Berio",
                    NickName = "Lucio",
                    Role = UserRole.Employee
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "federico.fellini@test.it",
                    Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                    FirstName = "Federico",
                    LastName = "Fellini",
                    NickName = "Fredo",
                    Role = UserRole.Employee
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "giorgio.moroder@test.it",
                    Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                    FirstName = "Giorgio",
                    LastName = "Moroder",
                    NickName = "Giorgio",
                    Role = UserRole.Employee
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "laura.pausini@test.it",
                    Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                    FirstName = "Laura",
                    LastName = "Pausini",
                    NickName = "Lau",
                    Role = UserRole.Employee
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "adriano.celentano@test.it",
                    Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                    FirstName = "Adriano",
                    LastName = "Celentano",
                    NickName = "Molleggiato",
                    Role = UserRole.Employee
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "michelangelo.antonioni@test.it",
                    Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                    FirstName = "Michelangelo",
                    LastName = "Antonioni",
                    NickName = "Miche",
                    Role = UserRole.Employee
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();

            // Generate work entries for the past 6 months
            InitializeWorkEntries(context, users);
        }

        private static void InitializeWorkEntries(PlurbyDbContext context, List<User> users)
        {
            if (context.WorkEntries.Any())
            {
                return; // Work entries already seeded
            }

            var workEntries = new List<WorkEntry>();
            var random = new Random();
            var startDate = DateTime.Now.AddMonths(-6);

            foreach (var user in users)
            {
                var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
                var endDate = DateTime.Now;

                while (currentDate <= endDate)
                {
                    // Generate work entries for weekdays (Monday-Friday)
                    if (currentDate.DayOfWeek >= DayOfWeek.Monday && currentDate.DayOfWeek <= DayOfWeek.Friday)
                    {
                        // Skip some random days to make it more realistic
                        if (random.Next(100) > 85) // 15% chance of skipping a day
                        {
                            currentDate = currentDate.AddDays(1);
                            continue;
                        }

                        var startTime = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 
                            random.Next(8, 10), random.Next(0, 60), 0);
                        
                        var workDuration = random.Next(6, 10); // 6-10 hours of work
                        var endTime = startTime.AddHours(workDuration);

                        workEntries.Add(new WorkEntry
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            StartTime = startTime,
                            EndTime = endTime
                        });
                    }

                    currentDate = currentDate.AddDays(1);
                }
            }

            context.WorkEntries.AddRange(workEntries);
            context.SaveChanges();
        }
    }
}
