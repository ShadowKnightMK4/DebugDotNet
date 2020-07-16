// TestCaseNullReference.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>

#include <Windows.h>
#include <DbgHelp.h>
int main()
{
    OutputDebugString(L"User Code Start\r\n");
    unsigned char* null_pt = NULL;
    int zero = 0;
    std::cout << "Hello World I'll be trieing to acess a null pointer!\n";
    
    __try
    {
     //   *null_pt = 0;
        ULONG_PTR Args[16];
        Args[0] = 1;
        Args[1] = 0;
        *null_pt = 0;
        std::cout << "Trying a divide by zero " << std::endl;
        zero = zero / zero;
        std::cout << "Rasing Acess Violation Now" << std::endl;
        RaiseException(EXCEPTION_ACCESS_VIOLATION, EXCEPTION_NONCONTINUABLE, 2, (const ULONG_PTR*)&Args);
    }
    __finally
    {
        std::cout << 1 / zero;
        OutputDebugString(L"Code is finished\r\n");
        ExitProcess(0);
    }
    return 0;
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
