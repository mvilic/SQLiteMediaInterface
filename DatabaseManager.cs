using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

public static class AllowedFileTypes {
    public static string music = "*.mp3,*.m4a,*.flac";
}

public static class Constants {
    public static string dbPath;
}

static class DatabaseManager{
    public static void InitialiseDatabase() {
        Constants.dbPath = @"URI=file:" + Directory.GetCurrentDirectory() + @"\library.db";
        string cs = Constants.dbPath;
        if (File.Exists(Directory.GetCurrentDirectory() + @"\library.db"))
            return;

        using var con = new SQLiteConnection(cs);
        con.Open();

        using var cmd = new SQLiteCommand(con);

        cmd.CommandText = "DROP TABLE IF EXISTS song";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS artist";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS album";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS genre";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS playlist";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS song_genre_relation";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS album_genre_relation";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS artist_genre_relation";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS song_playlist_relation";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE song(
            song_id INTEGER PRIMARY KEY,
            album INTEGER REFERENCES album,
            song_name TEXT, 
            song_number INTEGER,
            song_artist INTEGER REFERENCES artist,
            duration INTEGER,
            path TEXT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE artist(
            artist_id INTEGER PRIMARY KEY,
            artist_name TEXT, 
            artist_image_path TEXT )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE album(
            album_id INTEGER PRIMARY KEY,
            album_name TEXT,
            album_artist INTEGER REFERENCES artist,
            album_image_path TEXT,
            year_of_release INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE genre(
            genre_id INTEGER PRIMARY KEY,
            genre_name TEXT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE playlist(
            playlist_id INTEGER PRIMARY KEY,
            playlist_name TEXT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE song_genre_relation(
            song_id REFERENCES song,
            genre_id REFERENCES genre,
            PRIMARY KEY(song_id, genre_id))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE album_genre_relation(
            album_id REFERENCES album,
            genre_id REFERENCES genre,
            PRIMARY KEY(album_id, genre_id))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE artist_genre_relation(
            artist_id REFERENCES artist,
            genre_id REFERENCES genre,
            PRIMARY KEY(artist_id, genre_id))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE song_playlist_relation(
            song_id REFERENCES song,
            playlist_id REFERENCES playlist,
            PRIMARY KEY(song_id, playlist_id))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO artist(artist_name) VALUES ('Unknown Artist')"; cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO album(album_name) VALUES ('Unknown Album')"; cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO genre(genre_name) VALUES ('Unknown Genre')"; cmd.ExecuteNonQuery();

        Console.WriteLine("Database Created");
    }

    public static void AddFolderToDatabase(string FolderPath) {
        string cs = Constants.dbPath;

        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);
        con.Open();

        Song temp;

        foreach (string songFilePath in Directory.EnumerateFiles(FolderPath, "*.*", SearchOption.AllDirectories)
            .Where(s => AllowedFileTypes.music.Contains(Path.GetExtension(s).ToLower()))) {
            temp = new Song();
            PrepareFileForDatabase(temp, songFilePath);
            InsertSong(temp);
        }

        con.Close();
        cmd.Dispose();
    }

    public static void PrepareFileForDatabase(Song song, string songFilePath) {
        try{
            var file = TagLib.File.Create(songFilePath);
            
            song.path = songFilePath;

            if(file.Tag.Title!=null)
                song.name = file.Tag.Title;
            else
                song.name = Path.GetFileNameWithoutExtension(songFilePath);

            song.number = Convert.ToInt32(file.Tag.Track);
            song.album.name = file.Tag.Album;
            song.album.yearOfRelease = Convert.ToInt32(file.Tag.Year);

            if (file.Tag.FirstAlbumArtist == null){
                song.artist.name = file.Tag.FirstPerformer; 
            }
            else{
                song.artist.name = file.Tag.FirstAlbumArtist;
            }                

            song.duration = file.Properties.Duration;
            foreach (string genre in file.Tag.Genres){
                song.genres.Add(new Genre(genre));
            }
            if (song.genres.Count() == 0) {
                song.genres.Add(new Genre("Unknown Genre"));
            }
        }
        catch{
            Console.WriteLine("\nFile path error:");
            Console.WriteLine(songFilePath);

            song.path = songFilePath;
            song.name = Path.GetFileNameWithoutExtension(songFilePath);
            song.number = 0;
            song.album.name = null;
            song.album.yearOfRelease = 0;
            song.artist.name = null;
            song.duration = TimeSpan.FromMilliseconds(0);
            song.genres.Add(new Genre("Unknown Genre"));
        }
    }

    public static void InsertSong(Song song) { 
        List<string> genresFinal = new List<string>();
        string cs = Constants.dbPath;

        //TODO
            //Kod dodavanja u bazu artista i albuma sredi case sensitivity
            
        if (SongInDatabase(cs, song.path) 
            || !File.Exists(song.path)
            || !AllowedFileTypes.music.Contains(Path.GetExtension(song.path).ToLower()))
            return;

        //Setup default values
        if (Equals(song.album.name, "Unknown") || song.album.name==null)
            song.album.name = "Unknown Album";
        else
            song.album.name = song.album.name;

        if (Equals(song.artist.name, "Unknown") || song.artist.name==null)
            song.artist.name = "Unknown Artist";
        else
            song.artist.name = song.artist.name;

        foreach (Genre genre in song.genres) {
            if (Equals(genre, "Unknown") && !genresFinal.Exists(x => x.Contains("Unknown Genre")))
                genresFinal.Add("Unknown Genre");
            else
                genresFinal.Add(genre.name);
        }
        if (genresFinal.Count() == 0)
            genresFinal.Add("Unknown Genre");

        //If song artist doesn't exist in database, insert
        InsertArtist(cs, song.artist);

        //If song album doesn't exist in database, insert
        InsertAlbum(cs, song.album);        

        //If song genres don't exist in database, insert
        foreach (string genre in genresFinal) {
            InsertGenre(cs, genre);
        }

        //Create database connection and command string
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);

        //Push song into database
        con.Open();
        cmd.CommandText = "INSERT INTO song(album, song_name, song_number, song_artist, duration, path) VALUES (@album, @songName, @songNumber, @songArtist, @duration, @path)";
        cmd.Parameters.AddWithValue("@album", song.album.name);
        cmd.Parameters.AddWithValue("@songName", song.name);
        cmd.Parameters.AddWithValue("@songNumber", song.number);
        cmd.Parameters.AddWithValue("@songArtist", song.artist.name);
        cmd.Parameters.AddWithValue("@duration", song.duration.TotalMilliseconds);
        cmd.Parameters.AddWithValue("@path", song.path);
        cmd.ExecuteNonQuery();
        con.Close();

        //Take care of relations
        AddGenresToSong(song);
        AddGenresToAlbum(song.album, song.genres);
        AddGenresToArtist(song.artist, song.genres);
    
    }

    public static void InsertPlaylist(Playlist playlist) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);

        if (!PlaylistInDatabase(cs, playlist.name))
        {
            con.Open();
            cmd.CommandText = "INSERT INTO playlist(playlist_name) VALUES (@name)"; cmd.Parameters.AddWithValue("@name", playlist.name);
            cmd.ExecuteNonQuery();
            con.Close();
        }

        foreach(Song song in playlist.songs){
            AddSongToPlaylist(song, playlist);
        }
    }

    public static void InsertAlbum(string cs, string album) {
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);

        if (!Equals(album, "Unknown Album") && AlbumInDatabase(cs, album) == false)
        {
            con.Open();
            cmd.CommandText = "INSERT INTO album(album_name) VALUES (@album)"; cmd.Parameters.AddWithValue("@album", album);
            cmd.ExecuteNonQuery();
            con.Close();
        }
    }

    public static void AddSongToPlaylist(Song song, Playlist playlist) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);
        int returnedSongID, returnedPlaylistID;

        con.Open();
        cmd.CommandText = "SELECT song_id from song where path=@path"; cmd.Parameters.AddWithValue("@path", song.path); 
        var queryResult=cmd.ExecuteScalar();
        if (queryResult != null)
            returnedSongID = Convert.ToInt32(queryResult);
        else {
            InsertSong(song);
            returnedSongID = Convert.ToInt32(cmd.ExecuteScalar());
        }

        cmd.CommandText = "SELECT playlist_id from playlist WHERE playlist_name=@name"; cmd.Parameters.AddWithValue("@name", playlist.name);
        queryResult = cmd.ExecuteScalar();
        if (queryResult != null)
            returnedPlaylistID = Convert.ToInt32(queryResult);
        else {
            InsertPlaylist(playlist);
            returnedPlaylistID = Convert.ToInt32(cmd.ExecuteScalar());
        }

        if (!SongInPlaylist(returnedSongID, returnedPlaylistID)) {
            cmd.CommandText = "INSERT INTO song_playlist_relation(song_id, playlist_id) VALUES (@song_id, @playlist_id)";
            cmd.Parameters.AddWithValue("@song_id", returnedSongID);
            cmd.Parameters.AddWithValue("@playlist_id", returnedPlaylistID);
            cmd.ExecuteNonQuery();
        }

        con.Close();
    }

    public static void AddGenresToSong(Song song) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);
        int songID, genreID;

        if (!SongInDatabase(cs, song.path)) {
            InsertSong(song);
        }

        con.Open();
        foreach (Genre genre in song.genres) {
            InsertGenre(cs, genre.name);

            cmd.CommandText = "SELECT song_id FROM song WHERE path=@path"; cmd.Parameters.AddWithValue("@path", song.path);
            songID = Convert.ToInt32(cmd.ExecuteScalar());
            cmd.CommandText = "SELECT genre_id FROM genre WHERE genre_name=@genre"; cmd.Parameters.AddWithValue("@genre", genre.name);
            genreID = Convert.ToInt32(cmd.ExecuteScalar());

            if (!SongInGenre(songID, genreID)) {
                cmd.CommandText = "INSERT INTO song_genre_relation(song_id, genre_id) VALUES (@songID, @genreID)";
                cmd.Parameters.AddWithValue("@songID", songID);
                cmd.Parameters.AddWithValue("@genreID", genreID);
                cmd.ExecuteNonQuery();
            }            
        }
        con.Close();
    }

    public static void AddGenresToAlbum(Album album, List<Genre> genres) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);
        int albumID, genreID;

        if (!AlbumInDatabase(cs, album.name)){
            InsertAlbum(cs, album);
        }

        con.Open();
        cmd.CommandText = "SELECT album_id FROM album WHERE album_name=@album AND album_artist=@artist";
        cmd.Parameters.AddWithValue("@album", album.name);
        cmd.Parameters.AddWithValue("@artist", album.artist.name);
        albumID = Convert.ToInt32(cmd.ExecuteScalar());
       
         foreach (Genre genre in genres) {
            cmd.CommandText = "SELECT genre_id FROM genre WHERE genre_name=@genre";
            cmd.Parameters.AddWithValue("@genre", genre.name);
            genreID = Convert.ToInt32(cmd.ExecuteScalar());

            if (!AlbumInGenre(albumID, genreID)) {
                cmd.CommandText = "INSERT INTO album_genre_relation(album_id, genre_id) VALUES (@albumID, @genreID)";
                cmd.Parameters.AddWithValue("@albumID", albumID);
                cmd.Parameters.AddWithValue("@genreID", genreID);
                cmd.ExecuteNonQuery();
            }                
         }
        
        con.Close();
    }

    public static void AddGenresToArtist(Artist artist, List<Genre> genres) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);
        int artistID, genreID;

        if (!ArtistInDatabase(cs, artist.name)) {
            InsertArtist(cs, artist.name);
        }

        con.Open();
        cmd.CommandText = "SELECT artist_id FROM artist WHERE artist_name=@artist";
        cmd.Parameters.AddWithValue("@artist", artist.name);
        artistID = Convert.ToInt32(cmd.ExecuteScalar());

        foreach (Genre genre in genres) {
            cmd.CommandText = "SELECT genre_id FROM genre WHERE genre_name=@genre";
            cmd.Parameters.AddWithValue("@genre", genre.name);
            genreID = Convert.ToInt32(cmd.ExecuteScalar());

            if (!ArtistInGenre(artistID, genreID)) {
                cmd.CommandText = "INSERT INTO artist_genre_relation(artist_id, genre_id) VALUES (@artistID, @genreID)";
                cmd.Parameters.AddWithValue("@artistID", artistID);
                cmd.Parameters.AddWithValue("@genreID", genreID);
                cmd.ExecuteNonQuery();
            }
        }

        con.Close();
    }

    public static void InsertAlbum(string cs, Album album)
    {
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);

        if (!Equals(album.name, "Unknown Album") && AlbumInDatabase(cs, album.name) == false)
        {
            con.Open();
            cmd.CommandText = "INSERT INTO album(album_name, album_artist, album_image_path, year_of_release) VALUES (@albumName, @albumArtist, @imagePath, @yearOfRelease)"; 
            cmd.Parameters.AddWithValue("@albumName", album.name);

            if (Equals(album.imagePath, "Unknown Image"))
                cmd.Parameters.AddWithValue("@imagePath", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@imagePath", album.imagePath);

            if (Equals(album.yearOfRelease, "Unknown Year"))
                cmd.Parameters.AddWithValue("@yearOfRelease", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@yearOfRelease", album.yearOfRelease);

            if (Equals(album.artist.name, "Unknown Artist"))
                cmd.Parameters.AddWithValue("@albumArtist", "Unknown Artist");
            else
                cmd.Parameters.AddWithValue("@albumArtist", album.artist.name);

            cmd.ExecuteNonQuery();
            con.Close();
        }
    }

    public static void InsertArtist(string cs, string artist) {
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);

        if (!Equals(artist, "Unknown Artist") && ArtistInDatabase(cs, artist) == false)
        {
            con.Open();
            cmd.CommandText = "INSERT INTO artist(artist_name) VALUES (@artist)"; cmd.Parameters.AddWithValue("@artist", artist);
            cmd.ExecuteNonQuery();
            con.Close();
        }
    }

    public static void InsertArtist(string cs, Artist artist)
    {
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);

        if (!Equals(artist.name, "Unknown Artist") && ArtistInDatabase(cs, artist.name) == false)
        {
            con.Open();
            cmd.CommandText = "INSERT INTO artist(artist_name, artist_image_path) VALUES (@artist, @artistImage)"; 
            cmd.Parameters.AddWithValue("@artist", artist.name);
            if (Equals(artist.imagePath, "Unknown Image"))
                cmd.Parameters.AddWithValue("@artistImage", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@artistImage", artist.imagePath);
            cmd.ExecuteNonQuery();
            con.Close();
        }
    }

    public static void InsertGenre(string cs, string genre) {
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);

        if (!Equals(genre, "Unknown Genre") && GenreInDatabase(cs, genre) == false)
        {
            con.Open();
            cmd.CommandText = "INSERT INTO genre(genre_name) VALUES (@genre)"; cmd.Parameters.AddWithValue("@genre", genre);
            cmd.ExecuteNonQuery();
            con.Close();
        }
    }

    public static void InsertGenre(string cs, Genre genre)
    {
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);

        if (!Equals(genre.name, "Unknown Genre") && GenreInDatabase(cs, genre.name) == false)
        {
            con.Open();
            cmd.CommandText = "INSERT INTO genre(genre_name) VALUES (@genre)"; cmd.Parameters.AddWithValue("@genre", genre.name);
            cmd.ExecuteNonQuery();
            con.Close();
        }
    }

    public static Album GetAlbum(string albumToRetrieve) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); 
        using var cmd = new SQLiteCommand(con);
        Album toReturn = null;

        Artist artist = GetAlbumArtist(albumToRetrieve);
        
        cmd.CommandText = "SELECT *" +
            "FROM album " +
            "WHERE album_name=@album AND album_artist=@artist";
        cmd.Parameters.AddWithValue("@album", albumToRetrieve);
        cmd.Parameters.AddWithValue("@artist", artist.name);
        con.Open();
        using SQLiteDataReader reader = cmd.ExecuteReader();

        if (reader.Read()) {
            toReturn = new Album(artist);
            toReturn.name = reader.GetString(1);            
            if(reader.GetValue(3).ToString()=="")
                toReturn.imagePath = null;
            else
                toReturn.imagePath = reader.GetString(3);
            toReturn.yearOfRelease = reader.GetInt32(4);
        }
        reader.Close();

        cmd.CommandText = "SELECT COUNT(*) FROM album WHERE album_name=@album";
        cmd.Parameters.AddWithValue("@album", toReturn.name);
        toReturn.songCount = Convert.ToInt32(cmd.ExecuteScalar());

        con.Close();
        return toReturn;
    }

    public static Album GetSongAlbum(Song song) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs);
        using var cmd = new SQLiteCommand(con);
        Album toReturn = null;

        cmd.CommandText = "SELECT album_name, artist_name, album_image_path, year_of_release " +
            "FROM song INNER JOIN album ON song.album = album.album_name " +
            "INNER JOIN artist ON song.song_artist = artist.artist_name " +
            "WHERE path = @path";
        cmd.Parameters.AddWithValue("@path", song.path);
        con.Open();
        using SQLiteDataReader reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            Artist artist = new Artist(); artist.name = reader.GetString(1);
            toReturn = new Album(artist);
            toReturn.name = reader.GetString(0);
            if (reader.GetValue(2).ToString() == "")
                toReturn.imagePath = null;
            else
                toReturn.imagePath = reader.GetString(2);
            if (reader.GetValue(3) == DBNull.Value)
                toReturn.yearOfRelease = 0;
            else
                toReturn.yearOfRelease = reader.GetInt32(3);
        }
        reader.Close();

        toReturn.songCount = AlbumSongCount(toReturn);

        con.Close();
        return toReturn;
    }

    public static List<Song> GetAllSongs() {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);
        List<Song> toReturn = new List<Song>();
        Song temp;

        cmd.CommandText = "SELECT song_name, song_number, duration, path, artist_name, album_name, year_of_release " +
            "FROM song INNER JOIN album ON song.album=album.album_name " +
            "INNER JOIN artist ON song.song_artist=artist.artist_name ";       
        using SQLiteDataReader reader = cmd.ExecuteReader();

        while (reader.Read()) {
            temp = new Song();
            temp.name = reader.GetString(0);
            temp.number = reader.GetInt32(1);
            temp.duration = TimeSpan.FromMilliseconds(reader.GetDouble(2));
            temp.path = reader.GetString(3);
            temp.genres = GetSongGenres(temp);
            temp.artist.name = reader.GetString(4);
            temp.album.name = reader.GetString(5);
            temp.album.artist = temp.artist;
            temp.album.songCount = AlbumSongCount(temp.album);
            if (reader.GetValue(6) == DBNull.Value)
                temp.album.yearOfRelease = 0;
            else
                temp.album.yearOfRelease = reader.GetInt32(6);           

            toReturn.Add(temp);
            Console.WriteLine("Processed: "+reader.GetString(3));
        }

        con.Close();
        return toReturn;
    }

    public static List<Song> GetSongsByArtist(string artist) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);
        List<Song> toReturn = new List<Song>();
        Song temp;

        cmd.CommandText = "SELECT song_name, song_number, duration, path, artist_name, album_name, year_of_release " +
            "FROM song INNER JOIN album ON song.album=album.album_name " +
            "INNER JOIN artist ON song.song_artist=artist.artist_name " +
            "WHERE artist_name=@artist";
        cmd.Parameters.AddWithValue("@artist", artist);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        while (reader.Read()){
            temp = new Song();
            temp.name = reader.GetString(0);
            temp.number = reader.GetInt32(1);
            temp.duration = TimeSpan.FromMilliseconds(reader.GetDouble(2));
            temp.path = reader.GetString(3);
            temp.genres = GetSongGenres(temp);
            temp.artist.name = reader.GetString(4);
            temp.album.name = reader.GetString(5);
            temp.album.artist = temp.artist;
            temp.album.songCount = AlbumSongCount(temp.album);
            if (reader.GetValue(6) == DBNull.Value)
                temp.album.yearOfRelease = 0;
            else
                temp.album.yearOfRelease = reader.GetInt32(6);

            toReturn.Add(temp);
            Console.WriteLine("Processed: " + reader.GetString(3));
        }

        con.Close();
        return toReturn;
    }

    public static List<Song> GetSongsInPlaylist(string playlistName) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);
        List<Song> toReturn = new List<Song>();
        Song temp;

        cmd.CommandText = "SELECT song_name, song_number, duration, path, song_artist " +
            "FROM song_playlist_relation INNER JOIN song ON song_playlist_relation.song_id=song.song_id " +
            "INNER JOIN playlist ON song_playlist_relation.playlist_id=playlist.playlist_id " +            
            "WHERE playlist_name=@playlist";
        cmd.Parameters.AddWithValue("@playlist", playlistName);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        while (reader.Read()) {
            temp = new Song();
            temp.name = reader.GetString(0);
            temp.number = reader.GetInt32(1);
            temp.duration = TimeSpan.FromMilliseconds(reader.GetDouble(2));
            temp.path = reader.GetString(3);
            temp.genres = GetSongGenres(temp);
            temp.artist.name = reader.GetString(4);
            temp.album = GetSongAlbum(temp);
            temp.album.songCount = AlbumSongCount(temp.album);           

            toReturn.Add(temp);            
        }

        con.Close();
        return toReturn;
    }

    public static List<Album> GetAllAlbums() {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);
        List<Album> toReturn = new List<Album>();

        /*cmd.CommandText = @"CREATE TABLE album(
            album_id INTEGER PRIMARY KEY,
            album_name TEXT,
            album_artist INTEGER REFERENCES artist,
            album_image_path TEXT,
            year_of_release INT)";*/

        cmd.CommandText = "SELECT album_name, artist_name, album_image_path, year_of_release " +
            "FROM song INNER JOIN album ON song.album = album.album_name " +
            "INNER JOIN artist ON song.song_artist = artist.artist_name " +
            "GROUP BY album_name, artist_name";
        using SQLiteDataReader reader = cmd.ExecuteReader();

        while (reader.Read()) {
            var ret1 = reader.GetValue(0); var ret2 = reader.GetValue(1); var ret3 = reader.GetValue(2); var ret4 = reader.GetValue(3);
            Album temp = new Album(new Artist(reader.GetString(1)));
            temp.name = reader.GetString(0);
            if (reader.GetValue(2).ToString() == "")
                temp.imagePath = null;
            else
                temp.imagePath = reader.GetString(2);
            if (reader.GetValue(3) == DBNull.Value)
                temp.yearOfRelease = 0;
            else
                temp.yearOfRelease = reader.GetInt32(3);

            temp.songCount = AlbumSongCount(temp);
            toReturn.Add(temp);
        }

        con.Close();
        return toReturn;
    }

    public static List<Album> GetAlbumsByArtist(string artistName) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);
        List<Album> toReturn = new List<Album>();
        Album temp = null;

        cmd.CommandText = "SELECT album_name, artist_name, album_image_path, year_of_release " +
            "FROM song INNER JOIN album ON song.album = album.album_name " +
            "INNER JOIN artist ON song.song_artist = artist.artist_name " +
            "WHERE artist_name = @artistName " +
            "GROUP BY album_name";
        cmd.Parameters.AddWithValue("@artistName", artistName);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        while (reader.Read()) {
            temp = new Album();
            temp.name = reader.GetString(0);
            temp.artist.name = reader.GetString(1);
            if (reader.GetValue(2).ToString() == "")
                temp.imagePath = null;
            else
                temp.imagePath = reader.GetString(2);
            if (reader.GetValue(3) == DBNull.Value)
                temp.yearOfRelease = 0;
            else
                temp.yearOfRelease = reader.GetInt32(3);

            temp.songCount = AlbumSongCount(temp);

            toReturn.Add(temp);
        }
        con.Close();
        return toReturn;
    }

    public static List<Playlist> GetAllPlaylists() {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);
        List<Playlist> toReturn = new List<Playlist>();
        Playlist temp = null;

        cmd.CommandText = "SELECT playlist_name FROM playlist";
        using SQLiteDataReader reader = cmd.ExecuteReader();

        while (reader.Read()) {
            temp = new Playlist();
            temp.name = reader.GetString(0);
            temp.songs = GetSongsInPlaylist(temp.name);

            toReturn.Add(temp);
        }

        con.Close();
        return toReturn;
    }

    public static Playlist GetPlaylist(string playlistName) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);
        Playlist toReturn = new Playlist();

        cmd.CommandText = "SELECT playlist_name FROM playlist WHERE playlist_name=@playlistName";
        cmd.Parameters.AddWithValue("@playlistName", playlistName);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        if (reader.Read()){
            toReturn.name = reader.GetString(0);
            toReturn.songs = GetSongsInPlaylist(toReturn.name);
        }
        else
            return null;

        con.Close();
        return toReturn;
    }

    private static bool AlbumInDatabase(string cs, string album) {
        using var con = new SQLiteConnection(cs); con.Open();

        string stm = "SELECT count(*) FROM album WHERE album_name = @album";
        using var cmd = new SQLiteCommand(stm, con);
        cmd.Parameters.AddWithValue("@album", album);

        int count = Convert.ToInt32(cmd.ExecuteScalar());
        if (count == 0) {
            con.Close();
            return false;
        }
        else {
            con.Close();
            return true;
        }
    }

    private static bool ArtistInDatabase(string cs, string artist) {
        using var con = new SQLiteConnection(cs); con.Open();
     
        string stm = "SELECT count(*) FROM artist WHERE artist_name = @artist";
        using var cmd = new SQLiteCommand(stm, con);
        cmd.Parameters.AddWithValue("@artist", artist);

        int count = Convert.ToInt32(cmd.ExecuteScalar());
        if (count == 0) {
            con.Close();
            return false;
        }
        else {
            con.Close();
            return true;
        }
    }

    private static bool GenreInDatabase(string cs, string genre) {
        using var con = new SQLiteConnection(cs); con.Open();

        string stm = "SELECT count(*) FROM genre WHERE genre_name = @genre";
        using var cmd = new SQLiteCommand(stm, con);
        cmd.Parameters.AddWithValue("@genre", genre);

        int count = Convert.ToInt32(cmd.ExecuteScalar());
        if (count == 0) {
            con.Close();
            return false;
        }
        else {
            con.Close();
            return true;
        }
    }

    private static bool SongInDatabase(string cs, string songPath)
    {
        using var con = new SQLiteConnection(cs); con.Open();

        string stm = "SELECT count(*) FROM song WHERE path = @path";
        using var cmd = new SQLiteCommand(stm, con);
        cmd.Parameters.AddWithValue("@path", songPath);

        int count = Convert.ToInt32(cmd.ExecuteScalar());
        if (count == 0) {
            con.Close();
            return false; 
        }
        else {
            con.Close();
            return true; 
        }    
    }

    private static bool PlaylistInDatabase(string cs, string playlist)
    {
        using var con = new SQLiteConnection(cs); con.Open();

        string stm = "SELECT count(*) FROM playlist WHERE playlist_name = @name";
        using var cmd = new SQLiteCommand(stm, con);
        cmd.Parameters.AddWithValue("@name", playlist);

        int count = Convert.ToInt32(cmd.ExecuteScalar());
        if (count == 0)
        {
            con.Close();
            return false;
        }
        else
        {
            con.Close();
            return true;
        }

    }

    private static bool SongInPlaylist(int songID, int playlistID) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);

        cmd.CommandText = "SELECT COUNT (*) from song_playlist_relation WHERE song_id=@songID AND playlist_id=@playlistID";
        cmd.Parameters.AddWithValue("@songID", songID); cmd.Parameters.AddWithValue("@playlistID", playlistID);

        int count = Convert.ToInt32(cmd.ExecuteScalar());

        if (count == 0){
            con.Close();
            return false;
        }
        else{
            con.Close();
            return true;
        }
    }

    private static bool SongInGenre(int songID, int genreID)
    {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);

        cmd.CommandText = "SELECT COUNT (*) from song_genre_relation WHERE song_id=@songID AND genre_id=@genreID";
        cmd.Parameters.AddWithValue("@songID", songID); cmd.Parameters.AddWithValue("@genreID", genreID);

        int count = Convert.ToInt32(cmd.ExecuteScalar());

        if (count == 0)
        {
            con.Close();
            return false;
        }
        else
        {
            con.Close();
            return true;
        }
    }

    private static bool AlbumInGenre(int albumID, int genreID) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);

        cmd.CommandText = "SELECT COUNT (*) from album_genre_relation WHERE album_id=@albumID AND genre_id=@genreID";
        cmd.Parameters.AddWithValue("@albumID", albumID); cmd.Parameters.AddWithValue("@genreID", genreID);

        int count = Convert.ToInt32(cmd.ExecuteScalar());

        if (count == 0)
        {
            con.Close();
            return false;
        }
        else
        {
            con.Close();
            return true;
        }
    }

    private static bool ArtistInGenre(int artistID, int genreID)
    {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);

        cmd.CommandText = "SELECT COUNT (*) from artist_genre_relation WHERE artist_id=@artistID AND genre_id=@genreID";
        cmd.Parameters.AddWithValue("@artistID", artistID); cmd.Parameters.AddWithValue("@genreID", genreID);

        int count = Convert.ToInt32(cmd.ExecuteScalar());

        if (count == 0)
        {
            con.Close();
            return false;
        }
        else
        {
            con.Close();
            return true;
        }
    }

    public static int AlbumSongCount(Album album) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);

        cmd.CommandText = "SELECT COUNT(*) FROM song WHERE album=@album";
        cmd.Parameters.AddWithValue("@album", album.name);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public static Artist GetArtist(string artist) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);
        Artist toReturn = null;

        cmd.CommandText = "SELECT artist_name, artist_image_path FROM artist WHERE artist_name=@artist";
        cmd.Parameters.AddWithValue("@artist", artist);

        using SQLiteDataReader reader = cmd.ExecuteReader();

        if (reader.Read()){
            toReturn = new Artist();
            toReturn.name = reader.GetString(0);
            toReturn.imagePath = reader.GetString(1);
        }
        else
            return null;

        return toReturn;
    }

    public static List<Artist> GetAllArtists() {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);
        List<Artist> toReturn = new List<Artist>();

        cmd.CommandText = "SELECT artist_name, artist_image_path FROM artist";
        using SQLiteDataReader reader = cmd.ExecuteReader();

        while (reader.Read()){
            Artist temp = new Artist();
            temp.name = reader.GetString(0);

            if (reader.GetValue(1) == DBNull.Value)
                temp.imagePath = null;
            else
                temp.imagePath = reader.GetString(1);

            toReturn.Add(temp);
        }

        return toReturn;
    }

    public static Artist GetAlbumArtist(string album) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);

        cmd.CommandText = "SELECT song_artist FROM song WHERE album = @albumToRetrieve LIMIT 1";
        cmd.Parameters.AddWithValue("@albumToRetrieve", album);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        Artist toReturn = new Artist();
        if (reader.Read())
            toReturn.name=reader.GetString(0);        
        else
            return null;

        reader.Close();
        cmd.CommandText = "SELECT artist_image_path FROM artist WHERE artist_name=@artist";
        cmd.Parameters.AddWithValue("@artist", toReturn.name);
        string ret = cmd.ExecuteScalar().ToString();
        if (ret == "")
            toReturn.imagePath = null;
        else
            toReturn.imagePath = ret;

        return toReturn;
    }

    public static List<Song> GetAlbumSongs(Album album) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);
        int songID;
        List<Song> toReturn = new List<Song>();
        Song temp = null;

        cmd.CommandText = "SELECT song_name, song_number, duration, artist_name, path " +
            "FROM song INNER JOIN album on song.album=album.album_name " +
            "INNER JOIN artist on album.album_artist=artist.artist_name " +
            "WHERE album=@album";
        cmd.Parameters.AddWithValue("@album", album.name);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        while (reader.Read()) {
            temp = new Song(album.artist, album);
            temp.name = reader.GetString(0);
            temp.number = reader.GetInt32(1);
            temp.duration = TimeSpan.FromMilliseconds(reader.GetDouble(2));
            temp.artist.name = reader.GetString(3);
            temp.path = reader.GetString(4);
            toReturn.Add(temp);
        }
        reader.Close();


        foreach (Song song in toReturn) {
            song.genres = GetSongGenres(song);
        }

        return toReturn;
    }

    public static List<Genre> GetAlbumGenres(string albumName) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);
        List<Genre> toReturn=new List<Genre>();
        Genre temp = null;

        cmd.CommandText = "SELECT genre_name " +
            "FROM album_genre_relation INNER JOIN album ON album_genre_relation.album_id=album.album_id " +
            "INNER JOIN genre ON album_genre_relation.genre_id=genre.genre_ID " +
            "WHERE album_name=@albumName";
        cmd.Parameters.AddWithValue("@albumName", albumName);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        while (reader.Read()) {
            temp = new Genre(reader.GetString(0));
            toReturn.Add(temp);
        }

        con.Close();
        return toReturn;
    }

    public static List<Genre> GetSongGenres(Song song) {
        string cs = Constants.dbPath;
        using var con = new SQLiteConnection(cs); con.Open();
        using var cmd = new SQLiteCommand(con);
        List<Genre> toReturn = new List<Genre>();

        cmd.CommandText = "SELECT song_id FROM song WHERE path=@path"; cmd.Parameters.AddWithValue("@path", song.path);
        int songID = Convert.ToInt32(cmd.ExecuteScalar());

        cmd.CommandText = "SELECT genre_name " +
            "FROM song_genre_relation " +
            "INNER JOIN genre ON song_genre_relation.genre_id=genre.genre_id " +
            "WHERE song_id=@songID";
        cmd.Parameters.AddWithValue("@songID", songID);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        while (reader.Read()){            
            toReturn.Add(new Genre(reader.GetString(0)));
        }

        return toReturn;
    }
}