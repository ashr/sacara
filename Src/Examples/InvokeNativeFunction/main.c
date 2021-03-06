/**

This is an example program that use sacara VM in order to invoke a native function. The code assembled is the following one:

proc main
	push 0		   // number of bytes to remove from the native stack after the invocation
	push 0		   // number of arguments to push from the managed stack to the native stack
	push func_ptr  // this value is setted by the program
	ncall		   // invoke the function, we ignore the return value
	halt	 	   // end the VM execution
endp

To compile this code in VS, set Basic Runtime Checks settings to Default.

Generated with command: SacaraAsm.exe --gen-intermediate --gen-clang --encrypt-opcodes --encrypt-operands --multiple-opcodes test.sacara
*/


#include <stdio.h>
#include <stdint.h>
#include <Windows.h>

// this code was generated with the Sacara assembler
uint8_t code[] = {
	0xCF,0x4E,0xC7,0x37,0x72,0x52,           // /* CF4EC7377252 */ loc_00000000: VmPushImmediate 0x527237C7
	0xC7,0xF,                                // /* C70F         */ loc_00000006: VmAlloca 
	0xCF,0x4E,0xCE,0x3F,0x7A,0x5A,           // /* CF4ECE3F7A5A */ loc_00000008: VmPushImmediate 0x5A7A3FCE
	0xCF,0x4E,0xD4,0x25,0x60,0x40,           // /* CF4ED4256040 */ loc_0000000E: VmPushImmediate 0x406025D4
	0xFD,0x4D,0x48,0xD2,                     // /* FD4D48D2     */ loc_00000014: VmPushVariable 0xD248
	0xA0,0x2,                                // /* A002         */ loc_00000018: VmNativeCall 
	0xEC,0x3                                 // /* EC03         */ loc_0000001A: VmHalt 
};

typedef struct _vm_context {
	uint32_t *ip;
	uint32_t *stack;
	uint32_t status_flag;
	uint32_t *code;
	uint32_t code_size;
} vm_context;

typedef void (__stdcall *vm_init_func)(vm_context*, uint8_t[], uint32_t);
typedef uint32_t (__stdcall *vm_run_func)(vm_context*);
typedef void (__stdcall *vm_free_func)(vm_context*);
typedef void (__stdcall *vm_local_var_set_func)(vm_context*, uint32_t, uint32_t);
typedef uint32_t (__stdcall *vm_local_var_get_func)(vm_context*, uint32_t);

// VM functions
vm_init_func vm_init = NULL;
vm_run_func vm_run = NULL;
vm_free_func vm_free = NULL;
vm_local_var_set_func vm_local_var_set = NULL;
vm_local_var_get_func vm_local_var_get = NULL;

void resolve_vm_functions()
{
	HMODULE hModule = NULL;
	hModule = LoadLibrary("SacaraVm.dll");
	vm_init = (vm_init_func)GetProcAddress(hModule, "vm_init");
	vm_run = (vm_run_func)GetProcAddress(hModule, "vm_run");
	vm_free = (vm_free_func)GetProcAddress(hModule, "vm_free");
	vm_local_var_set = (vm_local_var_set_func)GetProcAddress(hModule, "vm_local_var_set");
	vm_local_var_get = (vm_local_var_get_func)GetProcAddress(hModule, "vm_local_var_get");
}

void hello_world() 
{
	printf("Hello from the Matrix!!");
}

int main()
{
	vm_context ctx = { 0 };	
	uint32_t result = 0;
	
	resolve_vm_functions();	

	// initialize the VM context structure
	vm_init(&ctx, code, sizeof(code));

	// add as local var the function address
	vm_local_var_set(&ctx, 0, (uint32_t)hello_world);

	// run the code
	result = vm_run(&ctx);

	// free the VM
	vm_free(&ctx);

	return result;
}