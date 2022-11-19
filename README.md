# Votify - Plugin for RaidMax's IW4MAdmin

Votify allows you to start or add to a vote on your server. Vote on map change, map skip, kicking a player or banning a player.

***

## Commands:
```
!votemap (!vm) <map> - Vote to change the map
!voteskip (!vs) - Vote to skip the current map
!votekick (!vk) <target> <reason> - Vote to kick a player
!voteban (!vb) <target> <reason> - Vote to ban a player (temporary, 1 hour)
!yes (!y) - Vote yes to an in progress vote
!no (!n) - Vote no to an in progress vote

!cancelvote (!cv) - Cancels the current in progress vote (Moderator)
```

Note, the configuration will create/update on load. Changes made to the configuration will need IW4MAdmin restarted.

Configuration is in `Configuration/VotifySettings.json`

Suggestions? Contact Ayymoss#8334 on Discord.

***

## Configuration:
```

  "IsEnabled": true, // Enable or Disable the plugin.
  "VotePassPercentage": 0.51, // Percentage of yes to no votes required to pass the vote.
  "MinimumPlayersRequired": 4, // Minimum numbers of players required to initiate a vote.
  "MinimumPlayersRequiredForSuccessfulVote": 3, // Minimum number of votes required for a successful vote.
  "VoteDuration": 30, // Length (Seconds) of time the vote runs for.
  "VoteCooldown": 60, // Length (Seconds) of time before the next vote can be initiated.
  "TimeBetweenVoteReminders": 5, // Length (Seconds) between each server announcement of the vote.
  "IsVoteTypeEnabled": {
    "VoteBan": false, // Enable or Disable the ability to vote ban.
    "VoteKick": true, // Enable or Disable the ability to vote kick.
    "VoteMap": true, // Enable or Disable the ability to vote map change.
    "VoteSkip": true // Enable or Disable the ability to vote map skip.
  },
```
