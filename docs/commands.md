# Commands List

This is the list of currently supported commands in MariBot.

### Basic and Unsorted Commands 
`info` - Displays bot information text.  
`flipcoin` - Flips a coin.  
`radar` - Gets the current radar animation for the Pacific Northwest.

### Audio Commands
`tts <text>` - Speaks a string in the voice channel you currently are in using DecTalk. Replace `<text>` with your string.  
`radio <text>` - Plays a specified radio station in the voice channel you currently are in. See radio.md for list of default supported webcams.

### fAPI Commands
⏲ These commands are subject to daily rate limits defined by dreadful.tech.

`4chan` - 🔞**NSFW**. Gets a 4Chan thread, random thread from board, or random board and thread. To specify a specific board or thread, use `4chan <path>` where path is in the form `board/thread`. For example `4chan vp` or `4chan vp/12345`  
`9gag <url>` - Apply 9gag filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`adidas <url>` - Apply adidas filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`adw <url>` - Apply adw filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`ajit <url>` - Apply Ajit Pai filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`america <url>` - Apply America filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`analysis <url>` - Apply Kolakowski Analysis filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`austin <url>` - Apply Austin Evans filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`autism <url>` - Apply Autism filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`bandicam <url>` - Apply Bandicam filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`bernie <url>` - Apply Bernie Sanders filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`binoculars <url>` - Apply Binoculars filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.
`blackify <url>` - Apply Blackify face filter to image. Relies on face detection. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`blackpanther <url>` - Apply Black Panther filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`bobross <url>` - Apply Bob Ross filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel. 
`buzzfeed <text>` - Creates an image of a BuzzFeed article. Replace `<text>` with article title or leave blank for a random title.
`cmm <text>` - Creates a Change My Mind image. Replace `<text>` with any text.  
`composite <url>` - Apply Composite filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`condom <url>` - Apply Condom filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel. 
 

### Image Commands
`comic <text>` - Displays a specified comic. Replace `<text>` with your choice. See comics.md for supported comics.  
`webcam <text>` - Displays a specified webcam. Replace `<text>` with your choice. See webcams.md for list of default supported webcams.  
`sonicsays <text>` - Generates a Sonic Says image. Replace `<text>` with any text.  
`r34 <tags>` - 🔞**NSFW**. Returns a random image from Rule34. Replace `<tags>` with your search tags. Use `_` instead of spaces in individual tags. Uses spaces to separate multiple tags.

### Urban Dictionary Commands
`urban <text>` - Displays a random Urban Dictionary definition for the specified word. Replace `<text>` with your word.  
`urbantop <text>` - Displays the top Urban Dictionary definition for the specified word. Replace `<text>` with your word.

### Wikipedia Commands
`wiki <text>` - Gets the specified Wikipedia article and displays the first section. The first result from a search will be used. Replace `<text>` with article name or search term.  
`wikisearch <text>` - Performs a search on Wikipedia and returns a list of found articles. Replace `<text>` with your search term.  

## Experimental Commands
⚠The following commands exist but are not considered feature complete and may have serious bugs.

### Basic and Unsorted Commands 
`reminder <text>&<datetime>` - Sets a reminder. Replace `<text>` with your reminder text and `<datetime>` with the date and/or time to remind you at. `<datetime>` must be in a standard format supported by .NET Framework.

### Game Commands
Append all commands in this category with `game`. For example, `game <command>` where `<command>` is a command from this category.

`balance` - Display your credit balance.  
`daily` - Get a daily credit bonus.  

### YouTube Commands
`yt <text>` - Plays audio from a YouTube video in the current voice channel you are connected to. Replace `<text>` with a valid YouTube URL or video name to search for.  
`yt-d <text>` - Gets the duration of a video. Replace `<text>` with a valid YouTube URL or video name to search for.  
`ytskip` - Skip the current video playing in the voice channel.

