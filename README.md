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
The configuration is commented. Please see what each property does. 
[Configuration.cs](Votify/Configuration/ConfigurationBase.cs)
