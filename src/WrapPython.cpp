// https://docs.python.org/3/extending/extending.html#a-simple-example

#define PY_SSIZE_T_CLEAN
#include "Python.h"
#include "frameobject.h"
#include <stdio.h>
#include <stdlib.h>
#include <algorithm>
#include <string>

extern "C" {
#include "acme.h"
#include "input.h"
#include "global.h"
#include "flow.h"
#include "WrapPython.h"
}

static input_t sInputContext;

static char *unexpectedPythonType = "Unexpected python type";

static void commonSetupDebug(PyObject *self, PyObject *args)
{
#if 1
	PyFrameObject *fo = PyEval_GetFrame();
	//	fo = PyFrame_GetBack(fo);	// This seems to cause PyTraceBack_Here to throw an exception
	PyTraceBack_Here(fo);
	PyObject * exc;
	PyObject * val;
	PyObject * tb;
	PyErr_Fetch(&exc, &val, &tb);
	// Note: This line seems to be accurate and includes the common header we add on
	int line = PyLong_AsLong(PyObject_GetAttrString(PyObject_GetAttrString(tb, "tb_frame"), "f_lineno"));
	// This will usually (but not always) be "<string>" because we use PyRun_SimpleString()
	const char * filename = PyUnicode_AsUTF8(PyObject_GetAttrString(PyObject_GetAttrString(PyObject_GetAttrString(tb, "tb_frame"), "f_code"), "co_filename"));
	if (filename[0] == '<')
	{
		Input_now->line_number += line;
	}
	else
	{
		Input_now->line_number = line;
		Input_now->original_filename = filename;
	}

#endif
}

static PyObject *acme_source(PyObject *self, PyObject *args)
{
	// Setup a fake input source for the assembler to use
	input_t	new_input = sInputContext;
	Input_now = &new_input;
	commonSetupDebug(self , args);

	char *command;

	if (!PyArg_ParseTuple(args, "s", &command))
	{
		Throw_error(unexpectedPythonType);
		return NULL;
	}

	std::string theSource = command;
	std::replace( theSource.begin(), theSource.end(), '\t', ' ');
	// Hack in extra source termination
	char *finalBuffer = (char *)malloc(theSource.length() + 10);
	strcpy(finalBuffer , theSource.c_str());
	for (size_t i = 0 ; i < theSource.length() ; i++)
	{
		if (finalBuffer[i] == 0x0d)
		{
			finalBuffer[i] = CHAR_EOS;
		}
		else if (finalBuffer[i] == 0x0a)
		{
			finalBuffer[i] = CHAR_EOS;
		}
	}
	finalBuffer[theSource.length() + 1] = CHAR_EOF;
	finalBuffer[theSource.length() + 2] = CHAR_EOF;
	finalBuffer[theSource.length() + 3] = CHAR_EOF;
	Input_now->src.ram_ptr = finalBuffer;
	Parse_until_eob_or_eof();

	free(finalBuffer);

	return PyLong_FromLong(0);
}

// Outputs full source for a byte value, which can be slow. It is needed because all the logic for handling the program counter is included in the buffer parser.
void Output_8b_source(int value)
{
	char theLine[128];
	sprintf(theLine , "!by %d" , value);
	int len = strlen(theLine);
	theLine[len] = CHAR_EOS;
	theLine[len+1] = CHAR_EOF;
	theLine[len+2] = '\0';
	Input_now->src.ram_ptr = theLine;
	Input_now->source_is_ram = true;
	Input_now->state = INPUTSTATE_NORMAL;
	Parse_until_eob_or_eof();
}

static PyObject *acme_bytenum(PyObject *self, PyObject *args)
{
	// Setup a fake input source for the assembler to use
	input_t	new_input = sInputContext;
	Input_now = &new_input;
	commonSetupDebug(self , args);

	int theInt;

	if (!PyArg_ParseTuple(args, "i", &theInt))
	{
		Throw_error(unexpectedPythonType);
		return NULL;
	}

	Output_8b_source(theInt);

	return PyLong_FromLong(0);
}

static PyObject *acme_bytestr(PyObject *self, PyObject *args)
{
	// Setup a fake input source for the assembler to use
	input_t	new_input = sInputContext;
	Input_now = &new_input;
	commonSetupDebug(self , args);

	char *command;
	if (!PyArg_ParseTuple(args, "s", &command))
	{
		Throw_error(unexpectedPythonType);
		return NULL;
	}
	char *originalCommand = command;

//	printf("bytestr: %s\n" , command);

	if (strncmp(command , "bytearray(" , 10) == 0)
	{
		command += 10;
	}

	if (strncmp(command , "b'" , 2) == 0 || strncmp(command , "b\"" , 2) == 0)
	{
		char endQuoteChar = command[1];
		command += 2;
		while (command[0] != endQuoteChar)
		{
			if (command[0] == '\\')
			{
				if (command[1] == 'x')
				{
					command+=2;
					char temp[1024];
					int pos = 0;
					temp[0] = command[0];
					temp[1] = command[1];
					temp[2] = '\0';
					Output_8b_source(strtol(temp , 0 , 16));
					command+=2;
					continue;
				}
				if (command[1] == '\'' || command[1] == '\\')
				{
					Output_8b_source(command[1]);
					command+=2;
					continue;
				}
				if (command[1] == 'b')
				{
					Output_8b_source(0x08);
					command+=2;
					continue;
				}
				if (command[1] == 't')
				{
					Output_8b_source(0x09);
					command+=2;
					continue;
				}
				if (command[1] == 'n')
				{
					Output_8b_source(0x0a);
					command+=2;
					continue;
				}
				if (command[1] == 'r')
				{
					Output_8b_source(0x0d);
					command+=2;
					continue;
				}
				// https://www.w3schools.com/python/gloss_python_escape_characters.asp
				printf("****EEK: %s\n" , command);
				Throw_serious_error("Unexpected python encoding type");
			}
			Output_8b_source(command[0]);
			command++;
		}
//		PyMem_Free(originalCommand);
		return PyLong_FromLong(0);
	}
	if (command[0] == '[')
	{
		command++;
		char *tok = strtok(command,",]");
		while (tok != 0)
		{
			Output_8b_source(atoi(tok));
			tok = strtok(0,",]");
		}
//		PyMem_Free(originalCommand);
		return PyLong_FromLong(0);
	}

	Output_8b_source(atoi(command));

//	PyMem_Free(originalCommand);
	return PyLong_FromLong(0);
}

static PyMethodDef AcmeMethods[] = {
	{"source",  acme_source, METH_VARARGS, "Adds source code to be assembled."},
	{"bytenum",  acme_bytenum, METH_VARARGS, "Adds byte(s) to output as number."},
	{"bytestr",  acme_bytestr, METH_VARARGS, "Adds byte(s) to output as string."},
	{NULL, NULL, 0, NULL}        /* Sentinel */
};

static struct PyModuleDef acmemodule = {
    PyModuleDef_HEAD_INIT,
    "acme",   /* name of module */
    NULL, /* module documentation, may be NULL */
    -1,       /* size of per-interpreter state of the module,
                 or -1 if the module keeps state in global variables. */
    AcmeMethods
};

PyMODINIT_FUNC PyInit_acme(void)
{
	return PyModule_Create(&acmemodule);
}

extern "C" int RunScript_Python(const char *parameters , const char *name , const char *python)
{
	sInputContext = *Input_now;

    wchar_t *program = Py_DecodeLocale(name, NULL);
    if (program == NULL)
	{
        fprintf(stderr, "Fatal error: cannot decode '%s'\n" , name);
        exit(1);
    }

    /* Add a built-in module, before Py_Initialize */
    if (PyImport_AppendInittab("acme", PyInit_acme) == -1)
	{
        fprintf(stderr, "Error: could not extend in-built modules table\n");
        exit(1);
    }

	// Source preamble...
	std::string fullSource;
	fullSource.append("import sys\n");
	for (int i = 0 ; i < gNumLibraryIncludes ; i++)
	{
		fullSource.append("sys.path.insert(0, '");
		fullSource.append(gLibraryIncludes[i]);
		fullSource.append("')\n");
	}
	fullSource.append("import acme\n");
	fullSource.append("acmeParameters = (");
	fullSource.append(parameters);
	fullSource.append(")\n");
	// ... until this point, include the real source
	int lineAdjust = std::count(fullSource.begin() , fullSource.end() , '\n');
	sInputContext.line_number -= lineAdjust;
	sInputContext.line_number--;
	fullSource.append(python);

    /* Pass argv[0] to the Python interpreter */
    Py_SetProgramName(program);

    /* Initialize the Python interpreter.  Required.
       If this step fails, it will be a fatal error. */
    Py_Initialize();

    /* Optionally import the module; alternatively,
       import can be deferred until the embedded script
       imports it. */
    PyObject *pmodule = PyImport_ImportModule("acme");
    if (!pmodule)
	{
        PyErr_Print();
        fprintf(stderr, "Error: could not import module 'acme'\n");
    }

	PyRun_SimpleString(fullSource.c_str());

	if (Py_FinalizeEx() < 0)
	{
		exit(120);
	}

    PyMem_RawFree(program);
    return 0;
}
