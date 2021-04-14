using System;
using System.Collections.Generic;
using System.Text;



public class Album{
    public string name { get; set; }
    public string imagePath { get; set; }
    public int yearOfRelease { get; set; }
    public int songCount { get; set; }
    public Artist artist { get; set; }    

    public Album()
    {
        artist = new Artist();        
    }

    public Album(Artist artist)
    {
        this.artist = artist;       
    }
}

