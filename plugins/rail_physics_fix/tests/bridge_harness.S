// Test-only Win64 harness: execute bridges against synthetic vehicle memory.
.intel_syntax noprefix
.text
.globl test_back
test_back:
    ret
.globl test_branch
test_branch:
    mov dword ptr [rip+test_branch_taken], 1
    ret

// A callee may overwrite ALL 32 bytes of its caller-provided shadow space.
.macro SHADOW
    mov qword ptr [rsp+8],  0x1111
    mov qword ptr [rsp+16], 0x2222
    mov qword ptr [rsp+24], 0x3333
    mov qword ptr [rsp+32], 0x4444
.endm
.globl mock_brake
mock_brake:
    SHADOW
    jmp test_brake_helper
.globl mock_slope
mock_slope:
    SHADOW
    jmp test_slope_helper
.globl mock_curve
mock_curve:
    SHADOW
    jmp test_curve_helper

// void run_bridge(void* entry, void* vehicle, void* snapshot)
.globl run_bridge
run_bridge:
    push rbp
    push rbx
    push rsi
    push rdi
    push r13
    sub rsp, 0xA0
    mov [rsp+0x20], rcx
    mov [rsp+0x28], r8
    movaps [rsp+0x40], xmm6
    movaps [rsp+0x50], xmm9
    movaps [rsp+0x60], xmm10
    movaps [rsp+0x70], xmm15
    mov rsi, rdx
    mov rdi, rdx
    mov rbp, rdx
    xor ebx, ebx
    xor r13d, r13d
    movss xmm0, dword ptr [rip+f2]
    movss xmm1, dword ptr [rip+f11]
    movss xmm2, dword ptr [rip+f12]
    movss xmm3, dword ptr [rip+f13]
    movss xmm4, dword ptr [rip+f14]
    movss xmm5, dword ptr [rip+f15]
    movss xmm6, dword ptr [rip+f100]
    movss xmm9, dword ptr [rip+f80]
    movss xmm10, dword ptr [rip+fSlope]
    movss xmm15, dword ptr [rip+f10]
    mov rax, 0x1111222233334444
    mov rcx, 0x2222333344445555
    mov rdx, 0x3333444455556666
    mov r8,  0x4444555566667777
    mov r9,  0x5555666677778888
    mov r10, 0x6666777788889999
    mov r11, 0x777788889999AAAA
    stc
    call qword ptr [rsp+0x20]
    pushfq
    pop qword ptr [rsp+0x30]
    mov rdi, [rsp+0x28]
    movss [rdi], xmm0
    movss [rdi+4], xmm6
    movss [rdi+8], xmm9
    movss [rdi+12], xmm1
    movss [rdi+16], xmm2
    movss [rdi+20], xmm3
    movss [rdi+24], xmm4
    movss [rdi+28], xmm5
    mov [rdi+32], rax
    mov [rdi+40], rcx
    mov [rdi+48], rdx
    mov [rdi+56], r8
    mov [rdi+64], r9
    mov [rdi+72], r10
    mov [rdi+80], r11
    mov rax, [rsp+0x30]
    mov [rdi+88], rax
    movaps xmm6, [rsp+0x40]
    movaps xmm9, [rsp+0x50]
    movaps xmm10, [rsp+0x60]
    movaps xmm15, [rsp+0x70]
    add rsp, 0xA0
    pop r13
    pop rdi
    pop rsi
    pop rbx
    pop rbp
    ret
.section .rdata,"dr"
f2: .float 2
f11: .float 11
f12: .float 12
f13: .float 13
f14: .float 14
f15: .float 15
f100: .float 100
f80: .float 80
fSlope: .float -0.5
f10: .float 10
