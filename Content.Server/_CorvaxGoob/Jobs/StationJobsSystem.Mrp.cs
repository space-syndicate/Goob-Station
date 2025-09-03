using System.Collections.Generic;
using System.Linq;
using Content.Server.Station.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    /// <summary>
    ///     Applies MRP visibility rules to the station job list.
    ///     mrpEnabled:
    ///         true  -> hide jobs with mrp == false (explicitly non-MRP)
    ///         false -> hide jobs with mrp == true  (explicitly MRP-only)
    ///         null  -> neutral, not hidden here
    /// </summary>
    public void ApplyMrpJobsFilter(EntityUid station, bool mrpEnabled)
    {
        StationJobsComponent? jobs = null;
        if (!Resolve(station, ref jobs, false))
            return;

        var toRemove = new List<ProtoId<JobPrototype>>();
        foreach (var jobId in jobs.JobList.Keys.ToList())
        {
            if (!_prototypes.TryIndex(jobId, out var proto))
                continue;

            if (proto.Mrp is null)
                continue;

            if ((mrpEnabled && proto.Mrp == false) || (!mrpEnabled && proto.Mrp == true))
                toRemove.Add(jobId);
        }

        foreach (var job in toRemove)
            jobs.JobList.Remove(job);

        jobs.TotalJobs = jobs.JobList.Values.Select(x => x ?? 0).Sum();
        UpdateJobsAvailable();
    }

    // Backwards-compat shim for previous callsites
    public void RemoveMrpJobs(EntityUid station)
        => ApplyMrpJobsFilter(station, false);
}
