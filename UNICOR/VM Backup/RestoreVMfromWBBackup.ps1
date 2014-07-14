# RestoreVMfromWBBackup.ps1
# Microsoft Consulting Services
# END USER LICENSE AGREEMENT ("EULA")
# By using the software, you accept these terms. If you do not accept them, do not use the
# software.
# If you comply with these license terms, you have the rights below.
# 1. INSTALLATION AND USE RIGHTS. One user may install and use any number of copies of the
#    software for internal use only. The software was developed for a customer engagement.
# 2. ADDITIONAL LICENSING REQUIREMENTS AND/OR USE RIGHTS. Right to use and distribute.
#	 The code listed below is "Distributable code."
# DISCLAIMER OF WARRANTY. The software or script  is licensed "as-is." You bear the risk 
# of using it. Microsoft gives no express warranties, guarantees or conditions. You may 
# have additional consumer rights under your local laws which this agreement cannot change.
# To the extent permitted under your local laws, Microsoft excludes the implied warranties
# of merchantability, fitness for a particular purpose and non-infringement.
#
# // History:
# // 1.0.2013.05.05	MCS	Created initial version


param(
    [string]$BackupShare = "\\usmds85001.linkedsolutions.corp\backup\usmdsv5001", 
    [switch]$AlternatePath,
    [string]$VMName
)
if (!([string]::IsNullOrEmpty($VMName))) {
    $BackupFolder=$BackupShare + "\" + $VMName
    if ((Test-Path -Path $BackupFolder) -eq $True) {
        Write-Host "Backup path is valid: $BackupFolder"
    }else{
        Write-Warning "The backup path $BackupFolder is not valid.  Verify the VM name and path before trying again."
        exit
    }
}else{
    if ((Test-path -Path $BackupShare) -eq $true) {
        $VMList = (Get-ChildItem -Path $BackupShare).Name
        Write-Host "The following VM backups were located:"
        foreach ($VMDir in $VMList) {
            Write-Host $VMDir
        }
        do {
            $VMLocated = $false
            $VMName = Read-Host -Prompt "Enter the VM name to be recovered from the backup set"
            foreach ($VMDir in $VMList) {
                if ($VMDir -imatch $VMName) {
                    $VMLocated = $True
                }
            }
        }
        until ($VMLocated -eq $True)
        $BackupFolder=$BackupShare + "\" + $VMName
        if ((Test-Path -Path $BackupFolder) -eq $True) {
            Write-Host "Backup path is valid: $BackupFolder"
        }else{
            Write-Warning "The backup path $BackupFolder is not valid.  Verify the VM name and path before trying again."
            exit
        }
    }else{ 
        Write-Warning "The backup path $BackupShare is not valid.  Verify the path and try again."
        exit
    }
}

$BackupLocation = New-WBBackupTarget -NetworkPath $BackupFolder -NonInheritAcl:$false -Verbose
$Backup = Get-WBBackupSet -BackupTarget $BackupLocation
Write-Host "Backup set located:"
Write-Host $Backup
if ($AlternatePath.IsPresent) {
    $TargetPath = Read-Host -Prompt "Enter the fully qualified alternate path to restore the VM to"
    Start-WBHyperVRecovery -BackupSet $Backup -VMInBackup $Backup.Application[0].Component[1] -TargetPath $TargetPath -UseAlternateLocation -RecreatePath
}else{
    Write-Host "Executing a recovery to the original location"
    Start-WBHyperVRecovery -BackupSet $Backup -VMInBackup $Backup.Application[0].Component[0]
}