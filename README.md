FastestFileEnumerator
=====================

When a folder contains a number of files, it gets slower to enumerate them. This package helps to enumerate them faster than DirectoryInfo and FileInfo classes in C#.Net.

## Sample Usage:

	List<FileData> files = FastEnumerator.EnumerateFiles(@"C:\Windows\System32", "*").ToList();
