# Multiplayer Testing Guide

This project supports multiplayer testing for Blackjack and Texas Hold'em Poker through one running app instance.

## What multiplayer currently supports

- User registration and login with unique friend codes
- Sending and accepting friend requests from the Dashboard
- Hosting Blackjack and Poker tables from their game lobbies
- `Private` tables for solo play
- `Public` tables that are visible to the host's added friends
- Joining a friend's live table from the Dashboard
- Wallet-backed bets, payouts, and poker stack settlement

## Important local testing rule

All players must connect to the same running app instance.

Example:

- Start the app once with `dotnet run`
- Use the same URL for every test user, such as `http://localhost:5138`

Do not start two separate `dotnet run` processes for two users, because the live room runtime is stored in the app process.

## Best ways to test two accounts locally

Use any of these:

1. Normal browser + incognito/private window
2. Chrome profile + Edge profile
3. Chrome normal window + Chrome guest profile
4. Two different computers on the same network hitting the same app URL

## Full demo flow

### 1. Start the app

From the project folder:

```powershell
cd Chuds2Chads
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```

Open the URL shown in the terminal.

### 2. Create two accounts

Use two separate browser sessions for:

- `Player A`
- `Player B`

Register both accounts and log in.

### 3. Add each other as friends

On `Player A`:

- Open `/Dashboard`
- Copy the friend code from the top-right

On `Player B`:

- Open `/Dashboard`
- Paste `Player A`'s friend code into `Add Friend By Code`
- Send request

Back on `Player A`:

- Accept/ignore the friend request

After refresh, both accounts should appear in each other's friends list.

### 4. Test Blackjack multiplayer

On `Player A`:

- Open `/BlackjackLobby`
- Create a `Public` table
- Wait on the lobby or enter the table

On `Player B`:

- Open `/Dashboard`
- Find `Player A` in the friends section or the live tables section
- Join the active Blackjack table

Expected result:

- Both users land in the same Blackjack room
- Each player can place a bet
- The round starts once all required players have bet
- Both players can see shared table state
- Round results settle to each user's wallet

### 5. Test Poker multiplayer

On `Player A`:

- Open `/PokerLobby`
- Create a `Public` table

On `Player B`:

- Open `/Dashboard`
- Join `Player A`'s Poker table

Expected result:

- Both users land in the same Poker room
- They share the same hand state, pot, turn order, and community cards
- The game advances as each user acts
- Stack changes settle back into the wallet when the hand is settled or the user leaves the table

### 6. Test private-table behavior

For each game:

- Create a `Private` table from the game lobby

Expected result:

- Blackjack starts after the solo player places a bet
- Private Poker starts as solo quick-play with the existing bot flow
- No other user should see or join that private table from the Dashboard

### 7. Test table deletion

On the host account Dashboard:

- Find a table you created
- Click `Delete`

Expected result:

- If the table is in the lobby, it closes immediately
- If the table is already in progress, it is marked to close after the current round/hand
- Other players finish the current round/hand before the table closes

## Recommended smoke checklist

Use this checklist before demoing:

- Two users can register and log in
- Friend codes are different for both users
- Friend request send/accept works
- Blackjack public table is visible to friends
- Poker public table is visible to friends
- Joined users reach the same live game
- Bets and actions are reflected on both clients
- Wallet balances change after wins/losses
- Private tables remain solo and are not joinable by friends
- Host can delete owned tables from the Dashboard

## Troubleshooting

### A friend cannot see a public table

Check:

- both users are accepted friends
- the host created the table as `Public`
- both users are connected to the same app instance and same URL
- the friend's Dashboard or lobby has had a few seconds to refresh

### Users appear to be in different games

This usually means they are not using the same running server instance. Stop extra app processes and test against one `dotnet run`.

### Wallet does not look updated yet

Refresh the table or Dashboard after the round/hand settles or after leaving Poker. Settlement happens through the multiplayer service, not as a hardcoded display value.
