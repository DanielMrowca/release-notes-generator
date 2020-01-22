using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using LibGit2Sharp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReleaseNotes.Generator.HTML.Models;
using System.Reflection;
using ReleaseNotes.Generator.HTML.Extensions;

namespace ReleaseNotes.Generator.HTML
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                //Log.Logger = new LoggerConfiguration()
                //    .MinimumLevel.Debug()
                //    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                //    .Enrich.FromLogContext()
                //    .WriteTo.ColoredConsole(outputTemplate: "{Message:lj}{NewLine}{Exception}")
                //    .CreateLogger();

                var host = CreateHostBuilder(args).Build();
                WriteLogo();

                var result = Parser
                .Default
                .ParseArguments<Options>(args);

                if (result.Tag == ParserResultType.NotParsed)
                {
                    result.WithNotParsed<Options>((errors) =>
                    {
                        foreach (var error in errors)
                        {
                            Console.WriteLine(error);
                        }
                    });
                }
                else
                {
                    result.WithParsed(async (opt) => await GenerateReleaseNotes(opt, host.Services));
                }

                return;
            }
            catch (Exception ex)
            {
                //Log.Fatal(ex, "Release notes generated terminated unexpectedly.");
                Console.WriteLine(ex);
            }
            finally
            {
                //Log.CloseAndFlush();
            }

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            .ConfigureLogging((context, builder) =>
            {
                builder.AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Error);
            });


        static async Task GenerateReleaseNotes(Options options, IServiceProvider serviceProvider)
        {
            List<Commit> commits = new List<Commit>();
            bool isBetweenTagFilter = false;
            int endCommitIndex = 0;
            bool isLast = false;
            bool isMerge = false;
            bool excludeMerges = options.ExcludeMerges != null && (bool)options.ExcludeMerges;


            try
            {
                using (var repo = new Repository(options.RepositoryPath))
                {
                    var tags = repo.Tags.OrderByDescending(x => x.FriendlyName).ToList();
                    var branch = repo.Branches.SingleOrDefault(x => x.FriendlyName == options.BranchName);

                    if (branch == null)
                        throw new NotFoundException($"Specified branch '{options.BranchName}' was not found.");
                    Commands.Checkout(repo, branch);
                    isBetweenTagFilter = !string.IsNullOrWhiteSpace(options.StartTag) && !string.IsNullOrWhiteSpace(options.EndTag);

                    var commitFilter = new CommitFilter()
                    {
                        SortBy = CommitSortStrategies.Topological,
                    };

                    if (isBetweenTagFilter)
                    {
                        tags.AsserTagExists(options.StartTag).AsserTagExists(options.EndTag);
                        commitFilter.IncludeReachableFrom = tags.GetTag(options.StartTag);
                        commitFilter.ExcludeReachableFrom = tags.GetTag(options.EndTag);
                    }

                    var logs = repo.Commits.QueryBy(commitFilter).ToList();

                    if (logs != null & logs.Any())
                    {
                        var endCommitId = isBetweenTagFilter ? logs.LastOrDefault().Id.Sha : string.IsNullOrWhiteSpace(options.EndCommitId) ? ((Commit)tags[2].Target).Id.Sha : options.EndCommitId;
                        Commit endCommit = branch.Commits.FirstOrDefault(x => x.Id.Sha == endCommitId);
                        endCommitIndex = logs.IndexOf(endCommit);

                        for (int i = 0; i < endCommitIndex + 1; i++)
                        {
                            var commit = logs[i];
                            isLast = commit.Id == endCommit.Id;
                            isMerge = commit.Parents.Count() > 1;

                            if (!isMerge || (isMerge && !excludeMerges))
                                commits.Add(commit);

                            if (isLast)
                                break;
                        }
                    }


                    using (var serviceScope = serviceProvider.CreateScope())
                    {
                        var renderer = serviceScope.ServiceProvider.GetRequiredService<IViewRenderService>();


                        var html = await renderer.RenderToStringAsync("Views/ReleaseNotes.cshtml", new ReleaseNotesViewModel()
                        {
                            ReleaseName = options.ReleaseName,
                            Changes = commits,
                            ReleaseComment = options.ReleaseComment
                        });

                        string outputPath = string.Empty;

                        if (string.IsNullOrWhiteSpace(options.OutputFilePath))
                        {
                            var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            outputPath = Path.Combine(rootPath, "ReleaseNotes.html");
                        }
                        else
                        {
                            outputPath = options.OutputFilePath;
                        }

                        //if(!File.Exists(outputPath))
                        //{
                        //    throw new DirectoryNotFoundException($"Specified directory '{outputPath}' does not exists!");
                        //}


                        File.WriteAllText(outputPath, html, Encoding.UTF8);
                        Console.WriteLine($"Release notes was successfully generated to {outputPath}");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }
        }



        private static void WriteLogo()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" ");
            Console.WriteLine(" ");
            Console.WriteLine("     ██████╗░███████╗██╗░░░░░███████╗░█████╗░░██████╗███████╗  ███╗░░██╗░█████╗░████████╗███████╗░██████╗");
            Console.WriteLine("     ██╔══██╗██╔════╝██║░░░░░██╔════╝██╔══██╗██╔════╝██╔════╝  ████╗░██║██╔══██╗╚══██╔══╝██╔════╝██╔════╝");
            Console.WriteLine("     ██████╔╝█████╗░░██║░░░░░█████╗░░███████║╚█████╗░█████╗░░  ██╔██╗██║██║░░██║░░░██║░░░█████╗░░╚█████╗░");
            Console.WriteLine("     ██╔══██╗██╔══╝░░██║░░░░░██╔══╝░░██╔══██║░╚═══██╗██╔══╝░░  ██║╚████║██║░░██║░░░██║░░░██╔══╝░░░╚═══██╗");
            Console.WriteLine("     ██║░░██║███████╗███████╗███████╗██║░░██║██████╔╝███████╗  ██║░╚███║╚█████╔╝░░░██║░░░███████╗██████╔╝");
            Console.WriteLine("     ╚═╝░░╚═╝╚══════╝╚══════╝╚══════╝╚═╝░░╚═╝╚═════╝░╚══════╝  ╚═╝░░╚══╝░╚════╝░░░░╚═╝░░░╚══════╝╚═════╝░");
            Console.WriteLine(" ");
            Console.WriteLine("     ░██████╗░███████╗███╗░░██╗███████╗██████╗░░█████╗░████████╗░█████╗░██████╗░");
            Console.WriteLine("     ██╔════╝░██╔════╝████╗░██║██╔════╝██╔══██╗██╔══██╗╚══██╔══╝██╔══██╗██╔══██╗");
            Console.WriteLine("     ██║░░██╗░█████╗░░██╔██╗██║█████╗░░██████╔╝███████║░░░██║░░░██║░░██║██████╔╝");
            Console.WriteLine("     ██║░░╚██╗██╔══╝░░██║╚████║██╔══╝░░██╔══██╗██╔══██║░░░██║░░░██║░░██║██╔══██╗");
            Console.WriteLine("     ╚██████╔╝███████╗██║░╚███║███████╗██║░░██║██║░░██║░░░██║░░░╚█████╔╝██║░░██║");
            Console.WriteLine("     ░╚═════╝░╚══════╝╚═╝░░╚══╝╚══════╝╚═╝░░╚═╝╚═╝░░╚═╝░░░╚═╝░░░░╚════╝░╚═╝░░╚═╝");
            Console.WriteLine(" ");
            Console.WriteLine(" ");

            Console.ResetColor();
        }
    }

    public class Options
    {
        [Option('p', "repoPath", Required = true, HelpText = "Set physical path to git repository.")]
        public string RepositoryPath { get; set; }

        [Option('b', "branchName", Required = true, HelpText = "Set branch name.")]
        public string BranchName { get; set; }

        [Option('o', "outputPath", Required = false, HelpText = "Full output path with file name.")]
        public string OutputFilePath { get; set; }

        [Option("releaseName", Required = true, HelpText = "Set release name.")]
        public string ReleaseName { get; set; }

        [Option("releaseComment", Required = false, HelpText = "Set release comments.")]
        public string ReleaseComment { get; set; }

        [Option("excludeMerges", Default = true, HelpText = "Set false if do not exclude merge commit from release notes.")]
        public bool? ExcludeMerges { get; set; }

        [Option("endCommitId", HelpText = "Commit id to filter from head commit.")]
        public string EndCommitId { get; set; }

        [Option("startTag", HelpText = "Start tag name. You must specify endTag too!")]
        public string StartTag { get; set; }
        [Option("endTag", HelpText = "End tag name. You must specify startTag too!")]
        public string EndTag { get; set; }

    }
}

