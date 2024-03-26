using System.Collections.Concurrent;
using SharedLibraryCore.Database.Models;
using Votify.Enums;

namespace Votify.Models;

public class VoteBase
{
    public EFClient Initiator { get; set; } = null!; // TODO: .NET 8 use 'required' keyword
    public DateTimeOffset Created { get; set; } // TODO: .NET 8 use 'required' keyword
    public ConcurrentDictionary<EFClient, Vote> Votes { get; set; } = new();
    public int YesVotes => Votes.Count(x => x.Value is Vote.Yes);
    public int NoVotes => Votes.Count(x => x.Value is Vote.No);
}

/*
 *
 *  Command -> VoteReceived -> Processor -> EVENTS (OnVoteFailed, OnVoteSucceeded) (GENERIC : Constraint)
 *
 * VoteState/EventObject -> Failed / Succeeded (Subscription)
 *  -> VoteMapEventHandler (Action logic)
 *
 * VoteProcessingService -> Per Vote Type (VoteBase)
 * 
 *
 *Later: 
Suggestion for an in-game command feature, such as !vm n(Now) Fringe or !vm A(After) Fringe, for immediate or post-match map change.

Now:
Proposal of individual command cooldowns to avoid chaos, like setting !vm and !vs with different cooldown periods.

Make it so the person who initiates the vote is the one who actually bans/kicks actions the vote. Not just in message from IW4MAdmin.
    This is implemented - test

 *
 * 
 */
