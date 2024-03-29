# Requirements  

- [Git](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git)  
- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)  
- [MySQL](https://dev.mysql.com/downloads/mysql/) or [MariaDB](https://mariadb.org/download/?t=mariadb&p=mariadb)  


## <u>Install Git</u>  
### Debian-based  
```sudo apt install git-all```  
### Windows  
[https://git-scm.com/download/win](https://git-scm.com/download/win)  
### macOS  
Homebrew:
```
brew install git
```  
### [Other macOS Installations](https://git-scm.com/download/mac)  

<hr>

## <u>Install .NET 7 SDK</u>  
### Ubuntu  
Replace `{22,20,18}` with your respective major OS version  
```
wget https://packages.microsoft.com/config/ubuntu/{22,20,18}.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y apt-transport-https
sudo apt-get update
sudo apt-get install -y dotnet-sdk-7.0
```
### [Other Linux Distributions](https://learn.microsoft.com/en-us/dotnet/core/install/linux)  

### Windows  
x86:  
```https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.101-windows-x86-installer```  

x64:  
```https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.101-windows-x64-installer``` 

ARM64:  
```https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.101-windows-arm64-installer```

### macOS  
Intel:  
```https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.101-macos-x64-installer```

Apple Silicon:
```https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.101-macos-arm64-installer```

<hr>

## <u>Install SQL Database</u>  

### MySQL  
[https://dev.mysql.com/downloads/installer](https://dev.mysql.com/downloads/installer)  

or

### MariaDB  
[https://mariadb.org/download/?t=mariadb](https://mariadb.org/download/?t=mariadb)
