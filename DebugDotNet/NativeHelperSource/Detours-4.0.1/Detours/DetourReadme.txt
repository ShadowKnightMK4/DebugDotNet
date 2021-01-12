This was sources from the github published package for detours.

Visual Studio 2019 complained about certain function variable initialzation being skipping.
I just moved them to the top of the function when that happened.

This has been modified to export routines that DebugDotNet.dll imports for c#. The Project set be
setup to output to the same location that debugDotNet currently outputs too.