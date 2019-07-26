# PersianStemmer.Core
A .NET Core 2.2 implementation of original [PersianStemmer-CSharp](https://github.com/htaghizadeh/PersianStemmer-CSharp) project.

### Install

If you're using Visual Studio, run following command in Package Manager Console:
```ps
Install-Package PersianStemmer.Core
```

Or in dotnet command line:
```ps
add package PersianStemmer.Core
```


### Code Example
Use `DefaultPersianStemmer.Run(string)` method to get the stemmed word.

```csharp
using PersianStemmer.Core.Stemming.Persian;

var ps = new DefaultPersianStemmer();
ps.run("زیباست");
// زیبا
Console.WriteLine(ps.run("پدران"));
// پدر
```

*Notice: If you're using Dependency Injection in you project, register `DefaultPersianStemmer` as a singleton dependency.*

### Citation
If you use this software please cite the followings:

Taghi-Zadeh, Hossein and Sadreddini, Mohammad Hadi and Diyanati, Mohammad Hasan and Rasekh, Amir Hossein. 2015. *A New Hybrid Stemming Method for Persian Language*. In *Digital Scholarship in the Humanities*. The Oxford University Press.
[DOI](http://dx.doi.org/10.1093/llc/fqv053)
[Link](http://dsh.oxfordjournals.org/content/early/2015/11/06/llc.fqv053.abstract)

H. Taghi-Zadeh and M. H. Sadreddini and M. H. Dianaty and A. H. Rasekh. 2013. *A New Rule-Based Persian Stemmer Using Regular Expression (In Persian)*. In *Iranian Conference on Intelligent Systems (ICIS 2013)*, pages 401–407.
[Link](http://www.civilica.com/Paper-ICS11-ICS11_109.html)
