This app can be used to split Ghidra output in multiple files.

It's pretty simple and may produce errors, the process was done based on a Ghidra output that I had.

Simply drag and drop the ghidra file in the window and it'll split the data in:
* _STRUCTS.c: Contains the structs
* _ENUMS.c: Contains the enums
* libraries: Contains the functions separated in files by the upper-most class. Functions without a class are written to _MISC.c

NOTE: The window contains a big button. When clicked it'll do redo the process using the last file given. This is used for debugging, because dropping files is async and won't stop the process if there's an error.