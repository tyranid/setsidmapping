# SetSidMapping.
Simple tool to use LsaManageSidNameMapping get LSA to add or remove SID to name mappings.

To use you need to have SeTcbPrivilege and the SID you map names to must meet the following
criteria.

- The SID security authority must be NT (5)
- The first RID of the SID must be between 80 and 111.
- You must register a domain SID first.

## Examples

Add the domain SID ABC and a user SID.
`SetSidMapping.exe S-1-5-101=ABC S-1-5-101-1-2-3=ABC\User`

Remove the domain SID and all its related SIDs.
`SetSidMapping.exe -r ABC`