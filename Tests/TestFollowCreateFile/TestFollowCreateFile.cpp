#include <Windows.h>
#include <iostream>
using namespace std;

int main()
{
	std::string wait;
	cout << "This program opens a file C:\\Windows\\Temp\\discard.txt, creating it if possible and deletes it when done. " << endl;
	cout << "Purpose to to allow a person to walk a trace of the api path that CreateFileW follows" << endl;

	cout << "Opening discard.txt" << endl;

	HANDLE tmp = CreateFile(L"C:\\Windows\\Temp\\discard.txt", GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL | FILE_ATTRIBUTE_TEMPORARY, NULL);
	if (tmp != INVALID_HANDLE_VALUE)
	{
		cout << "file opened. Closing It now" << endl;
		CloseHandle(tmp);
	}
	else
	{
		cout << "did not open it ok." << endl;
	}
	cin >> wait;
		return 0;
}