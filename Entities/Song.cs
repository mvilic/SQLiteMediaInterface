using System;
using System.Collections.Generic;
using System.Text;


public class Song{
    public string name { get; set; }
    public int number { get; set; }
    public TimeSpan duration { get; set; }
    public string path { get; set; }
    public Artist artist;
    public Album album;
    public List<Genre> genres;

    public Song()
    {
        artist = new Artist();
        album = new Album(artist);
        genres = new List<Genre>();
    }

    public Song(Artist passedArtist = null, Album passedAlbum = null, List<Genre> passedGenres = null)
    {
        if (passedArtist == null)
            artist = new Artist();
        else
            artist = passedArtist;

        if (passedAlbum == null)
            album = new Album();
        else
            album = passedAlbum;

        if (passedGenres == null)
            genres = new List<Genre>();
        else
            genres = passedGenres;

    }
}

