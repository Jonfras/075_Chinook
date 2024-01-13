namespace ChinookEFDB.DbContext;

public partial class Playlist
{
    public virtual ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
}