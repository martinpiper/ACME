// https://docs.python.org/3/extending/extending.html#a-simple-example

#define PY_SSIZE_T_CLEAN
#include "Python.h"
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

static PyObject *acme_source(PyObject *self, PyObject *args)
{
	char *command;

	if (!PyArg_ParseTuple(args, "s", &command))
		return NULL;

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
	Input_now->line_number = 1;
	Input_now->src.ram_ptr = finalBuffer;
	Parse_until_eob_or_eof();

	free(finalBuffer);

	return PyLong_FromLong(0);
}

static PyMethodDef AcmeMethods[] = {
	{"source",  acme_source, METH_VARARGS, "Adds source code to be assembled."},
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


extern "C" int RunScript_Python(char *name , char *python)
{
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

	PyRun_SimpleString(python);

	if (Py_FinalizeEx() < 0)
	{
		exit(120);
	}

    PyMem_RawFree(program);
    return 0;
}