# statictext

Manage custom text responses for this server.

## Commands

- `z statictext add <key> <text>` — Add a new guild-specific response.
- `z statictext update <key> <text>` — Update an existing guild-specific response.
- `z statictext remove <key>` — Remove a guild-specific response.
- `z statictext list` — List all available static text commands (global and guild).
- `z statictext addglobal <key> <text>` *(Owner only)* — Add a global response available in all servers.
- `z statictext updateglobal <key> <text>` *(Owner only)* — Update a global response.
- `z statictext removeglobal <key>` *(Owner only)* — Remove a global response.
- `z statictext promote <key>` *(Owner only)* — Promote a guild response to global.

## Notes

- Responses can include text and/or attachments.
- Static text commands are invoked directly by their key (e.g., `z mykey`).
