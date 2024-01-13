namespace ChinookEFDB.DbContext;

public partial class Track
{
    public virtual ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
    public override string ToString() {
        return $"TrackId {TrackId} Name {Name} AlbumId {AlbumId} MediaTypeId {MediaTypeId} GenreId {GenreId}";
    }
}