using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

public class CreateTable
{
    static void Main()
    {
        //var file = TagLib.File.Create(@"D:\Music\Ayreon\1995 The Final Experiment\03 Eyes Of Time.mp3");
        //file = TagLib.File.Create(@"D:\Music\Avantasia\Moonglow (2019)\02. Book of Shallows.mp3");

        DatabaseManager.InitialiseDatabase();
        /*
        DatabaseManager.AddFolderToDatabase(@"D:\Music");

        Playlist pl = new Playlist();
        pl.name = "Nightwish Playlist";
        pl.songs = DatabaseManager.GetSongsByArtist("Nightwish");
        DatabaseManager.InsertPlaylist(pl);

        Playlist pl2 = new Playlist();
        pl2.name = "Ayreon Playlist";
        pl2.songs = DatabaseManager.GetSongsByArtist("Ayreon");
        DatabaseManager.InsertPlaylist(pl2);
        */
        /*Song sng = new Song();
        sng.album.name = "Age Of The Joker";
        sng.path = @"D:\Music\Edguy\Edguy - 2011 - Age Of The Joker\Cd-1\04. Pandora's Box.mp3";
        Album ret = DatabaseManager.GetSongAlbum(sng);*/

        /*Song song = new Song();
        song.path = @"D:\Music\Nightingale - Retribution  2014\02 - Lucifer's Lament.mp3";
        song.genres.Add(new Genre("Unknown Genre"));
        DatabaseManager.AddGenresToSong(song);*/
        var ret = DatabaseManager.GetPlaylist("Nightwish Playlist");


    }

    //TODO
        //Upiti
            //Sve pisme DONE
            //Svi albumi DONE
            //Pojedini album DONE
            //Svi artisti DONE
            //Pojedini artisti DONE
            //Sve pisme u albumu DONE
            //Sve pisme od artista DONE
            //Svi albumi od artista DONE
            //Broj pisama u albumu DONE
            //Zanrovi albuma DONE

        //Playliste
            //Sve DONE
            //Pojedine DONE
}