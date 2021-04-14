using System;
using System.Collections.Generic;
using System.Text;
public class Artist{
    public string name { get; set; }
    public string imagePath { get; set; }

    public Artist(string passedName) {
        name = passedName;
    }

    public Artist() { 
    }
}

