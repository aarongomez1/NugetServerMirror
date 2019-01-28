# NugetServerMirror
C# Windows Service that will mirror the contents of one NuGet server to another.

Use this at your own risk.  I wrote this in a little over an hour and tested it to make sure it worked. I'm using it in-house to mirror
packages across the world.

This works by using the Nuget-CLI.  The command-line tools must be installed and entered into your environment path variable. 
This tool uses nuget list, nuget install, and nuget push to read and write nuget packages.
It uses nuget list on both the source and target servers to find out what is installed.  It diffs those lists to find packages installed 
on the source that are missing on the target. It then installs the missing packages locally using nuget install.  Then pushes the 
packages to the target server using nuget push.

To start open the App.config file and configure the fields under the Mirror section to point to your source and target servers.  You must 
enter the API key for pushing to the target in the targetApiKey field.

