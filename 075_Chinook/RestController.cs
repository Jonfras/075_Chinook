using System.Runtime.Intrinsics.Arm;
using ChinookEFDB.DbContext;
using Microsoft.EntityFrameworkCore;

string corsKey = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddPolicy(
    corsKey,
    x => x.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
));


var connectionString = builder.Configuration.GetConnectionString("ChinookDb")!;
builder.Services.AddDbContext<ChinookContext>(options => options.UseSqlServer(connectionString));


var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(corsKey);

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/genres", async (ChinookContext db) => {
    var genres = await db.Genres.ToListAsync();
    genres.ForEach(x => Console.WriteLine(x.GenreId));
    return Results.Ok(genres);
});

app.MapGet("/genres/{genreId:int}", async (int genreId, ChinookContext db) => {
    var genre = await db.Genres.FindAsync(genreId);
    if (genre == null) {
        return Results.NotFound();
    }

    return Results.Ok(genre);
});

app.MapGet("/mediatypes", async (ChinookContext db) => {
    var mediaTypes = await db.MediaTypes.ToListAsync();
    return Results.Ok(mediaTypes);
});

app.MapGet("/mediatypes/{mediaTypeId:int}", async (int mediaTypeId, ChinookContext db) => {
    var mediaType = await db.MediaTypes.FindAsync(mediaTypeId);
    if (mediaType == null) {
        return Results.NotFound();
    }

    return Results.Ok(mediaType);
});

app.MapGet("/artists", async (ChinookContext db) => {
    var artists = await db.Artists.ToListAsync();
    return Results.Ok(artists);
});

app.MapGet("/albums", async (int artistId, ChinookContext db) => {
    var albums = await db.Albums.Where(a => a.ArtistId == artistId)
        .ToListAsync();
    albums.ForEach(x => Console.WriteLine(x));
    return Results.Ok(albums);
});

app.MapGet("/tracks", async (int albumId, ChinookContext db) => {
    var tracks = await db.Tracks.Where(t => t.AlbumId == albumId)
        .ToListAsync();
    tracks.ForEach(x => Console.WriteLine(x.TrackId));
    return Results.Ok(tracks);
});

app.MapDelete("tracks/{trackId:int}", async (int trackId, ChinookContext db) => {
    var track = await db.Tracks.FindAsync(trackId);
    if (track == null) {
        return Results.NotFound();
    }

    db.PlaylistTrack.Where(pt => pt.TrackId == trackId)
        .ToList()
        .ForEach(pt => db.PlaylistTrack.Remove(pt));


    track.Playlists.Clear();

    db.InvoiceLines.Where(il => il.TrackId == trackId)
        .ToList()
        .ForEach(il => db.InvoiceLines.Remove(il));

    track.InvoiceLines.Clear();


    var album = db.Albums.FirstOrDefault(a => a.AlbumId == track.AlbumId)
        ?.Tracks.Remove(track);
    Console.WriteLine($"Album: {album}");

    var genre = db.Genres.FirstOrDefault(g => g.GenreId == track.GenreId)
        ?.Tracks.Remove(track);
    Console.WriteLine($"Genre: {genre}");

    var mediatype = db.MediaTypes.FirstOrDefault(m => m.MediaTypeId == track.MediaTypeId)
        ?.Tracks.Remove(track);
    Console.WriteLine($"MediaType: {mediatype}");

    db.Tracks.Remove(track);

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapPost("/tracks", async (Track track, ChinookContext db) => {
    Console.WriteLine($"Track: {track}");
    var album = db.Albums.FirstOrDefault(a => a.AlbumId == track.AlbumId);
    Console.WriteLine(album);
    if (album == null) {
        return Results.BadRequest("Album not found");
    }

    var genre = await db.Genres.FindAsync(track.GenreId);
    if (genre == null) {
        return Results.BadRequest("Genre not found");
    }

    var mediaType = await db.MediaTypes.FindAsync(track.MediaTypeId);
    if (mediaType == null) {
        return Results.BadRequest("MediaType not found");
    }

    var newTrack = new Track {
        Name = track.Name,
        Album = album,
        Genre = genre,
        MediaType = mediaType,
    };
    db.Tracks.Add(track);
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapPut("/tracks/{trackId:int}", async (int trackId, Track track, ChinookContext db) => {
    var oldTrack = await db.Tracks.FindAsync(trackId);
    if (oldTrack == null) {
        return Results.NotFound();
    }

    var album = await db.Albums.FindAsync(track.AlbumId);
    if (album == null) {
        Console.WriteLine($"Album not found: {track.AlbumId}");
        return Results.BadRequest("Album not found");
    }

    var genre = await db.Genres.FindAsync(track.GenreId);
    if (genre == null) {
        Console.WriteLine($"Genre not found: {track.GenreId}");
        return Results.BadRequest("Genre not found");
    }

    var mediatype = await db.MediaTypes.FindAsync(track.MediaTypeId);
    if (mediatype == null) {
        Console.WriteLine($"MediaType not found: {track.MediaTypeId}");
        return Results.BadRequest("MediaType not found");
    }

    oldTrack.Name = track.Name;
    oldTrack.AlbumId = track.AlbumId;
    oldTrack.GenreId = track.GenreId;
    oldTrack.MediaTypeId = track.MediaTypeId;

    await db.SaveChangesAsync();
    return Results.NoContent();
});


app.Run();