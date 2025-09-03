using System.Collections.Generic;
using System.Linq;
using Content.Server.Station.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public void RemoveMrpJobs(EntityUid station)
    {
        StationJobsComponent? jobs = null;
        if (!Resolve(station, ref jobs, false))
            return;

        var toRemove = new List<ProtoId<JobPrototype>>();
        foreach (var jobId in jobs.JobList.Keys.ToList())
        {
            if (_prototypes.TryIndex(jobId, out var proto) && proto.Mrp)
                toRemove.Add(jobId);
        }

        foreach (var job in toRemove)
            jobs.JobList.Remove(job);

        jobs.TotalJobs = jobs.JobList.Values.Select(x => x ?? 0).Sum();
        UpdateJobsAvailable();
    }
}
