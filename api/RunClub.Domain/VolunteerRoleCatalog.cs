namespace RunClub.Domain;

public static class VolunteerRoleCatalog
{
    public static readonly (string Name, string Description)[] Defaults =
    [
        ("Marshal", "Station at a key point on the course"),
        ("Run lead", "Lead the group"),
        ("Car park attendant", "Direct parking before and after the event"),
        ("Timekeeper", "Record finish times"),
        ("Tail runner", "Stay at the back of the group"),
        ("Setup", "Help set up the start area"),
        ("Registration", "Sign runners in before the start")
    ];
}
