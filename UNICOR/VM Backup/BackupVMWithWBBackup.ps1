# BackupVMWithWBBackup.ps1
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
    [string]$BackupShare = "\\usmds85001.linkedsolutions.corp\backup$\usmdsv5001", 
    [switch]$AlternateCredentials,
    [string]$VMName
)

if ((Test-Path -Path $BackupShare) -eq $True) {
    Write-Host "Backup path is valid: $BackupShare"
}else{
    Write-Warning "The backup path $BackupShare is not valid.  Verify the VM name and path before trying again."
    exit
}

if ($AlternateCredentials.IsPresent) {
    $cred = (Get-Credential)
}

$ScriptBlock = {
    $targetfolder = Join-Path -Path $BackupShare -ChildPath "$($_.Name)"
    If (Test-Path $targetfolder) {
        Write-Verbose -Message "Previous backup for VM $($_.Name) found" -Verbose
        # Delete it first
        try {
            Remove-Item -Path $targetfolder -Force -ErrorAction Stop
        } catch {
            Write-Warning -Message "Failed to remove target folder $targetfolder"
            return
        }
        # Create directory
        try {
            New-Item -Path $targetfolder -ItemType Directory -ErrorAction Stop
        } catch {
            Write-Warning -Message "Failed to create target folder $targetfolder"
            return
        }
    } else {
        # Create directory
        try {
            New-Item -Path $targetfolder -ItemType Directory -ErrorAction Stop
        } catch {
            Write-Warning -Message "Failed to create target folder $targetfolder"
            return
        }
    }
 
    # Create an empty policy object            
    $BackupPolicy = New-WBPolicy
 
    # Add the VM
    Get-WBVirtualMachine | ? VMName -eq $_.Name | Add-WBVirtualMachine -Policy $BackupPolicy
     
    # Define a target folder
    if ($AlternateCredentials.IsPresent) {
        $targetvol = New-WBBackupTarget -NetworkPath $targetfolder -Credential $cred -NonInheritAcl:$false -Verbose
    }else{
        $targetvol = New-WBBackupTarget -NetworkPath $targetfolder -NonInheritAcl:$false -Verbose
    }
    Add-WBBackupTarget -Policy $BackupPolicy -Target $targetvol
 
    # Set a schedule
    Set-WBSchedule -Policy $BackupPolicy -Schedule ([datetime]::Now.AddMinutes(10))
 
    # Start the backup
    Start-WBBackup -Policy $BackupPolicy -Async
 
    # Let us know the status of the running job
    While((Get-WBJob).JobState -eq "Running") { 
        $percent = ([int](@(([regex]'.*\((?<percent>\d{2})\%\).*').Matches((Get-WBJob).CurrentOperation).Groups)[-1].Value))
        if ($percent) {
            Write-Progress -Activity "Backup of VM $($_.Name) in progress" -Status "Percent completed: " -PercentComplete $percent
        } else {
            Write-Progress -Activity "Backup of VM $($_.Name) in progress" -Status (Get-WBJob).CurrentOperation
        }
    }
}

If ([string]::IsNullOrEmpty($VMName)) {
    Get-VM | ForEach-Object -Process $ScriptBlock
}else{
    Get-VM -Name $VMName | ForEach-Object -Process $ScriptBlock
}