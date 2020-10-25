/////////////////////////////////////////////////////////////////////////////
// SetSidMapping - Example of using LsaManageSidNameMapping to add or
// remove name to SID mappings in LSA.
// Copyright (C) 2020 - James Forshaw.

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace SetSidMapping
{
    static class Program
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            ushort Length;
            ushort MaximumLength;
            [MarshalAs(UnmanagedType.LPWStr)]
            string Buffer;

            public UNICODE_STRING(string str)
            {
                Length = 0;
                MaximumLength = 0;
                Buffer = null;
                SetString(str);
            }

            public void SetString(string str)
            {
                if (str.Length > ushort.MaxValue / 2)
                {
                    throw new ArgumentException("String too long for UnicodeString");
                }
                Length = (ushort)(str.Length * 2);
                MaximumLength = (ushort)((str.Length * 2) + 1);
                Buffer = str;
            }
        }

        internal enum LSA_SID_NAME_MAPPING_OPERATION_TYPE
        {
            LsaSidNameMappingOperation_Add,
            LsaSidNameMappingOperation_Remove,
            LsaSidNameMappingOperation_AddMultiple,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct LSA_SID_NAME_MAPPING_OPERATION_ADD_INPUT
        {
            public UNICODE_STRING DomainName;
            public UNICODE_STRING AccountName;
            public IntPtr Sid;
            public int Flags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct LSA_SID_NAME_MAPPING_OPERATION_REMOVE_INPUT
        {
            public UNICODE_STRING DomainName;
            public UNICODE_STRING AccountName;
        }

        internal enum LSA_SID_NAME_MAPPING_OPERATION_ERROR
        {
            LsaSidNameMappingOperation_Success,
            LsaSidNameMappingOperation_NonMappingError,
            LsaSidNameMappingOperation_NameCollision,
            LsaSidNameMappingOperation_SidCollision,
            LsaSidNameMappingOperation_DomainNotFound,
            LsaSidNameMappingOperation_DomainSidPrefixMismatch,
            LsaSidNameMappingOperation_MappingNotFound,
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        internal static extern int LsaManageSidNameMapping(
            LSA_SID_NAME_MAPPING_OPERATION_TYPE OperationType,
            in LSA_SID_NAME_MAPPING_OPERATION_ADD_INPUT OperationInput,
            out IntPtr OperationOutput
        );

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        internal static extern int LsaManageSidNameMapping(
            LSA_SID_NAME_MAPPING_OPERATION_TYPE OperationType,
            in LSA_SID_NAME_MAPPING_OPERATION_REMOVE_INPUT OperationInput,
            out IntPtr OperationOutput
        );

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool ConvertStringSidToSidW(
            string StringSid,
            out IntPtr Sid
        );

        [DllImport("ntdll.dll")]
        public static extern int RtlNtStatusToDosErrorNoTeb(int status);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("Advapi32.dll", SetLastError = true)]
        internal static extern int LsaFreeMemory(IntPtr Buffer);

        static IntPtr ParseSid(string sid)
        {
            if (!ConvertStringSidToSidW(sid, out IntPtr ptr))
            {
                throw new ArgumentException($"Invalid SDDL SID {sid}");
            }
            return ptr;
        }

        static void RemoveSidName(string name)
        {
            LSA_SID_NAME_MAPPING_OPERATION_REMOVE_INPUT input = new LSA_SID_NAME_MAPPING_OPERATION_REMOVE_INPUT();
            IntPtr output = IntPtr.Zero;
            try
            {
                int index = name.IndexOf('\\');
                if (index >= 0)
                {
                    input.DomainName = new UNICODE_STRING(name.Substring(0, index));
                    input.AccountName = new UNICODE_STRING(name.Substring(index + 1));
                }
                else
                {
                    input.DomainName = new UNICODE_STRING(name);
                }

                int status = LsaManageSidNameMapping(LSA_SID_NAME_MAPPING_OPERATION_TYPE.LsaSidNameMappingOperation_Remove,
                    input, out output);
                if (status < 0)
                    throw new Win32Exception(RtlNtStatusToDosErrorNoTeb(status));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding {0} - {1}", name, ex.Message);
            }
            finally
            {
                if (output != IntPtr.Zero)
                    LsaFreeMemory(output);
            }
        }

        static void AddSidName(string name)
        {
            LSA_SID_NAME_MAPPING_OPERATION_ADD_INPUT input = new LSA_SID_NAME_MAPPING_OPERATION_ADD_INPUT();
            IntPtr output = IntPtr.Zero;
            try
            {
                string[] parts = name.Split('=');
                if (parts.Length != 2)
                {
                    throw new ArgumentException("Mapping must be SID=Name");
                }

                input.Sid = ParseSid(parts[0]);
                name = parts[1];
                int index = name.IndexOf('\\');
                if (index >= 0)
                {
                    input.DomainName = new UNICODE_STRING(name.Substring(0, index));
                    input.AccountName = new UNICODE_STRING(name.Substring(index + 1));
                }
                else
                {
                    input.DomainName = new UNICODE_STRING(name);
                }

                int status = LsaManageSidNameMapping(LSA_SID_NAME_MAPPING_OPERATION_TYPE.LsaSidNameMappingOperation_Add,
                    input, out output);
                if (status < 0)
                    throw new Win32Exception(RtlNtStatusToDosErrorNoTeb(status));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding {0} - {1}", name, ex.Message);
            }
            finally
            {
                if (input.Sid != IntPtr.Zero)
                {
                    LocalFree(input.Sid);
                }
                if (output != IntPtr.Zero)
                {
                    LsaFreeMemory(output);
                }
            }
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("SetSidMapping S-1-5-99=ABC S-1-5-99-1-2-3=ABC\\User - Add the domain ABC and a User.");
                Console.WriteLine("SetSidMapping -r ABC\\User - Remove mapping.");
                return;
            }

            if (args[0] == "-r")
            {
                foreach (var name in args.Skip(1))
                {
                    RemoveSidName(name);
                }
            }
            else
            {
                foreach (var name in args)
                {
                    AddSidName(name);
                }
            }
        }
    }
}
