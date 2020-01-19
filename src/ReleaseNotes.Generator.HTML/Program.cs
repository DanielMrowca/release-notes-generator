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
using System.Drawing;

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
            try
            {
                using (var repo = new Repository(options.RepositoryPath))
                {
                    var tags = repo.Tags.OrderByDescending(x => x.FriendlyName).ToList();
                    var branch = repo.Branches.SingleOrDefault(x => x.FriendlyName == options.BranchName);

                    var endCommitId = string.IsNullOrWhiteSpace(options.EndCommitId) ? ((Commit)tags[2].Target).Id.Sha : options.EndCommitId;
                    Commit startCommit = branch.Tip;
                    Commit endCommit = branch.Commits.FirstOrDefault(x => x.Id.Sha == endCommitId);

                    bool isLast = false;
                    bool isMerge = false;
                    bool excludeMerges = options.ExcludeMerges != null && (bool)options.ExcludeMerges;

                    var com = branch.Commits.OrderByDescending(x => x.Committer.When).ToList();
                    var startCommitIndex = com.IndexOf(startCommit);
                    var endCommitIndex = com.IndexOf(endCommit);

                    for (int i = startCommitIndex; i < endCommitIndex + 1; i++)
                    {
                        var commit = com[i];
                        isLast = commit.Id == endCommit.Id;
                        isMerge = commit.Parents.Count() > 1;

                        if (!isMerge || (isMerge && !excludeMerges))
                            commits.Add(commit);

                        if (isLast)
                            break;
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

        [Option("endCommitId", HelpText = "Set false if do not exclude merge commit from release notes.")]
        public string EndCommitId { get; set; }

    }
}

