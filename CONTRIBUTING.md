# Contributing to this repository

## Making Suggestions 
If you want to make a suggestion for linpeas or winpeas please use **[github issues](https://github.com/peass-ng/PEASS-ng/issues)**

## Do don't know how to help?
Check out the **[TODO](https://github.com/peass-ng/PEASS-ng/blob/master/TODO.md) page**

## Searching for files with sensitive information
From the PEASS-ng release **winpeas and linpeas are auto-built** and will search for files containing sensitive information specified in the **[sesitive_files.yaml](https://github.com/peass-ng/PEASS-ng/blob/master/build_lists/sensitive_files.yaml)** file.

If you want to **contribute adding the search of new files that can contain sensitive information**, please, just update **[sesitive_files.yaml](https://github.com/peass-ng/PEASS-ng/blob/master/build_lists/sensitive_files.yaml)** and create a **PR to master** (*linpeas and winpeas will be auto-built in this PR*). You can find examples of how to contribute to this file inside the file.
Also, in the comments of this PR, put links to pages where and example of the file containing sensitive information can be foud.

## Specific LinPEAS additions
From the PEASS-ng release **linpeas is auto-build from [linpeas/builder](https://github.com/peass-ng/PEASS-ng/blob/master/linPEAS/builder/)**. Therefore, if you want to contribute adding any new check for linpeas/macpeas, please **add it in this directory and create a PR to master**. *Note that some code is auto-generated in the python but most of it it's just written in different files that will be merged into linpeas.sh*.
The new linpeas.sh script will be auto-generated in the PR.

## Specific WinPEAS additions
Just modify winpeas and create a PR to master.
The new winpeas binaries will be auto-generated in the PR.
