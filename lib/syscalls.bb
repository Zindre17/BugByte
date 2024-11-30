aka read_id 0
# size buffer fd -> size
read(int ptr int)int: read_id syscall3;

aka open_id 2
# mode flags path -> fd | error
open(int int 0str)int: open_id syscall3;

aka close_id 3
# fd -> 0 | error
close(int)int: close_id syscall1;

aka stat_id 4
struct stat-data:
    st_dev 8
    st_ino 8
    st_nlink 8
    st_mode 4
    st_uid 4
    st_gid 4
    st_pad0 4
    st_rdev 8
    st_size int
    st_blksize 8
    st_blocks 8
    st_atim.tv_sec 8
    st_atim.tv_nsec 8
    st_mtim.tv_sec 8
    st_mtim.tv_nsec 8
    st_ctim.tv_sec 8
    st_ctim.tv_nsec 8
    __unused 24
;
# statbuf path -> 0 | error
stat(ptr 0str)int: stat_id syscall2;

aka fstat_id 5
# statbuf fd -> 0 | error
fstat(ptr int)int: fstat_id syscall2;

aka mmap_id 9
aka MAP_PRIVATE 2
aka PROT_READ 1
#  offset fd flags prot len addr  -> addr | error
mmap(int int int int int ptr)ptr: mmap_id syscall6 as ptr;

aka socket_id 41
aka AF_INET 2
aka SOCK_STREAM 1
aka DEFAULT_PROTOCOL 0
# protocol type domain -> fd | error
socket(int int int)int: socket_id syscall3 ;

aka connect_id 42
struct sockaddr_in:
    sin_family 2
    sin_port 2
    sin_addr 4
    sin_zero 8
;
# addrlen addr sockfd -> 0 | error
connect(int ptr int)int: connect_id syscall3 ;

aka accept_id 43
# addrlen addr sockfd -> 0 | error
accept(int ptr int)int: accept_id syscall3 ;

aka sendto_id 44
# addrlen addr flags msglength msgbuffer sockfd -> 0 | error
sendto(int ptr int int ptr int)int: sendto_id syscall6 ;


aka bind_id 49
# addrlen addr sockfd -> 0 | error
bind(int ptr int)int: bind_id syscall3 ;

aka listen_id 50
# backlog sockfd -> 0 | error
listen(int int)int: listen_id syscall2 ;


aka setsockopt_id 54
setsockopt(int ptr int int int)int: setsockopt_id syscall5 ;

aka write_id 1
aka lstat_id 6
aka poll_id 7
aka lseek_id 8
aka mprotect_id 10
aka munmap_id 11
aka brk_id 12
aka rt_sigaction_id 13
aka rt_sigprocmask_id 14
aka rt_sigreturn_id 15
aka ioctl_id 16
aka pread64_id 17
aka pwrite64_id 18
aka readv_id 19
aka writev_id 20
aka access_id 21
aka pipe_id 22
aka select_id 23
aka sched_yield_id 24
aka mremap_id 25
aka msync_id 26
aka mincore_id 27
aka madvise_id 28
aka shmget_id 29
aka shmat_id 30
aka shmctl_id 31
aka dup_id 32
aka dup2_id 33
aka pause_id 34
aka nanosleep_id 35
aka getitimer_id 36
aka alarm_id 37
aka setitimer_id 38
aka getpid_id 39
aka sendfile_id 40
aka recvfrom_id 45
aka sendmsg_id 46
aka recvmsg_id 47
aka shutdown_id 48
aka getsockname_id 51
aka getpeername_id 52
aka socketpair_id 53
aka getsockopt_id 55
aka clone_id 56
aka fork_id 57
aka vfork_id 58
aka execve_id 59
aka exit_id 60
aka wait4_id 61
aka kill_id 62
aka uname_id 63
aka semget_id 64
aka semop_id 65
aka semctl_id 66
aka shmdt_id 67
aka msgget_id 68
aka msgsnd_id 69
aka msgrcv_id 70
aka msgctl_id 71
aka fcntl_id 72
aka flock_id 73
aka fsync_id 74
aka fdatasync_id 75
aka truncate_id 76
aka ftruncate_id 77
aka getdents_id 78
aka getcwd_id 79
aka chdir_id 80
aka fchdir_id 81
aka rename_id 82
aka mkdir_id 83
aka rmdir_id 84
aka creat_id 85
aka link_id 86
aka unlink_id 87
aka symlink_id 88
aka readlink_id 89
aka chmod_id 90
aka fchmod_id 91
aka chown_id 92
aka fchown_id 93
aka lchown_id 94
aka umask_id 95
aka gettimeofday_id 96
aka getrlimit_id 97
aka getrusage_id 98
aka sysinfo_id 99
aka times_id 100
aka ptrace_id 101
aka getuid_id 102
aka syslog_id 103
aka getgid_id 104
aka setuid_id 105
aka setgid_id 106
aka geteuid_id 107
aka getegid_id 108
aka setpgid_id 109
aka getppid_id 110
aka getpgrp_id 111
aka setsid_id 112
aka setreuid_id 113
aka setregid_id 114
aka getgroups_id 115
aka setgroups_id 116
aka setresuid_id 117
aka getresuid_id 118
aka setresgid_id 119
aka getresgid_id 120
aka getpgid_id 121
aka setfsuid_id 122
aka setfsgid_id 123
aka getsid_id 124
aka capget_id 125
aka capset_id 126
aka rt_sigpending_id 127
aka rt_sigtimedwait_id 128
aka rt_sigqueueinfo_id 129
aka rt_sigsuspend_id 130
aka sigaltstack_id 131
aka utime_id 132
aka mknod_id 133
aka uselib_id 134
aka personality_id 135
aka ustat_id 136
aka statfs_id 137
aka fstatfs_id 138
aka sysfs_id 139
aka getpriority_id 140
aka setpriority_id 141
aka sched_setparam_id 142
aka sched_getparam_id 143
aka sched_setscheduler_id 144
aka sched_getscheduler_id 145
aka sched_get_priority_max_id 146
aka sched_get_priority_min_id 147
aka sched_rr_get_interval_id 148
aka mlock_id 149
aka munlock_id 150
aka mlockall_id 151
aka munlockall_id 152
aka vhangup_id 153
aka modify_ldt_id 154
aka pivot_root_id 155
aka _sysctl_id 156
aka prctl_id 157
aka arch_prctl_id 158
aka adjtimex_id 159
aka setrlimit_id 160
aka chroot_id 161
aka sync_id 162
aka acct_id 163
aka settimeofday_id 164
aka mount_id 165
aka umount2_id 166
aka swapon_id 167
aka swapoff_id 168
aka reboot_id 169
aka sethostname_id 170
aka setdomainname_id 171
aka iopl_id 172
aka ioperm_id 173
aka create_module_id 174
aka init_module_id 175
aka delete_module_id 176
aka get_kernel_syms_id 177
aka query_module_id 178
aka quotactl_id 179
aka nfsservctl_id 180
aka getpmsg_id 181
aka putpmsg_id 182
aka afs_syscall_id 183
aka tuxcall_id 184
aka security_id 185
aka gettid_id 186
aka readahead_id 187
aka setxattr_id 188
aka lsetxattr_id 189
aka fsetxattr_id 190
aka getxattr_id 191
aka lgetxattr_id 192
aka fgetxattr_id 193
aka listxattr_id 194
aka llistxattr_id 195
aka flistxattr_id 196
aka removexattr_id 197
aka lremovexattr_id 198
aka fremovexattr_id 199
aka tkill_id 200
aka time_id 201
aka futex_id 202
aka sched_setaffinity_id 203
aka sched_getaffinity_id 204
aka set_thread_area_id 205
aka io_setup_id 206
aka io_destroy_id 207
aka io_getevents_id 208
aka io_submit_id 209
aka io_cancel_id 210
aka get_thread_area_id 211
aka lookup_dcookie_id 212
aka epoll_create_id 213
aka epoll_ctl_old_id 214
aka epoll_wait_old_id 215
aka remap_file_pages_id 216
aka getdents64_id 217
aka set_tid_address_id 218
aka restart_syscall_id 219
aka semtimedop_id 220
aka fadvise64_id 221
aka timer_create_id 222
aka timer_settime_id 223
aka timer_gettime_id 224
aka timer_getoverrun_id 225
aka timer_delete_id 226
aka clock_settime_id 227
aka clock_gettime_id 228
aka clock_getres_id 229
aka clock_nanosleep_id 230
aka exit_group_id 231
aka epoll_wait_id 232
aka epoll_ctl_id 233
aka tgkill_id 234
aka utimes_id 235
aka vserver_id 236
aka mbind_id 237
aka set_mempolicy_id 238
aka get_mempolicy_id 239
aka mq_open_id 240
aka mq_unlink_id 241
aka mq_timedsend_id 242
aka mq_timedreceive_id 243
aka mq_notify_id 244
aka mq_getsetattr_id 245
aka kexec_load_id 246
aka waitid_id 247
aka add_key_id 248
aka request_key_id 249
aka keyctl_id 250
aka ioprio_set_id 251
aka ioprio_get_id 252
aka inotify_init_id 253
aka inotify_add_watch_id 254
aka inotify_rm_watch_id 255
aka migrate_pages_id 256
aka openat_id 257
aka mkdirat_id 258
aka mknodat_id 259
aka fchownat_id 260
aka futimesat_id 261
aka newfstatat_id 262
aka unlinkat_id 263
aka renameat_id 264
aka linkat_id 265
aka symlinkat_id 266
aka readlinkat_id 267
aka fchmodat_id 268
aka faccessat_id 269
aka pselect6_id 270
aka ppoll_id 271
aka unshare_id 272
aka set_robust_list_id 273
aka get_robust_list_id 274
aka splice_id 275
aka tee_id 276
aka sync_file_range_id 277
aka vmsplice_id 278
aka move_pages_id 279
aka utimensat_id 280
aka epoll_pwait_id 281
aka signalfd_id 282
aka timerfd_create_id 283
aka eventfd_id 284
aka fallocate_id 285
aka timerfd_settime_id 286
aka timerfd_gettime_id 287
aka accept4_id 288
aka signalfd4_id 289
aka eventfd2_id 290
aka epoll_create1_id 291
aka dup3_id 292
aka pipe2_id 293
aka inotify_init1_id 294
aka preadv_id 295
aka pwritev_id 296
aka rt_tgsigqueueinfo_id 297
aka perf_event_open_id 298
aka recvmmsg_id 299
aka fanotify_init_id 300
aka fanotify_mark_id 301
aka prlimit64_id 302
aka name_to_handle_at_id 303
aka open_by_handle_at_id 304
aka clock_adjtime_id 305
aka syncfs_id 306
aka sendmmsg_id 307
aka setns_id 308
aka getcpu_id 309
aka process_vm_readv_id 310
aka process_vm_writev_id 311
aka kcmp_id 312
aka finit_module_id 313
aka sched_setattr_id 314
aka sched_getattr_id 315
aka renameat2_id 316
aka seccomp_id 317
aka getrandom_id 318
aka memfd_create_id 319
aka kexec_file_load_id 320
aka bpf_id 321
aka execveat_id 322
aka userfaultfd_id 323
aka membarrier_id 324
aka mlock2_id 325
aka copy_file_range_id 326
aka preadv2_id 327
aka pwritev2_id 328
aka pkey_mprotect_id 329
aka pkey_alloc_id 330
aka pkey_free_id 331
aka statx_id 332
