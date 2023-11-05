namespace Hafnia.DataAccess.Models;

public record TagFilter
(
    IEnumerable<string> Include,
    IEnumerable<string> Exclude
);
