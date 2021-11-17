# Commands List

This is the list of currently supported commands by MariBot. All commands should start with the configured prefix followed by the command and any parameters. Example: `z info`  

⚠Pardon the Dust  
A large chunk of commands powered by fAPI were lost due to it's sudden deprecation. A number of the commands will be reimplemented at a later time.

### Basic and Unsorted Commands 
`info` - Displays bot information text.  
`flipcoin` - Flips a coin.  
`google <text>` - Performs a Google search. Replace `<text>` with your keywords to search.  
`kg <text>` - Performs a Google Knowledge Graph search. Replace `<text>` with your keywords to search.  
`latex <text>` - Render LaTeX. Replace `<text>` with your LaTeX. The bot also will automatically run this command when a message containing an equation surrounded by `$$` is detected.   
`radar` - Gets the current radar animation for the Pacific Northwest.  
`solve <equation>` - Try to solve an equation. Replace `<equation>` with your equation to be solved. Limited support. 

### Audio Commands
`tts <text>` - Speaks a string in the voice channel you currently are in using DecTalk. Replace `<text>` with your string.  
`radio <text>` - Plays a specified radio station in the voice channel you currently are in. See [radio.md](radio.md) for list of default supported webcams.  

### Booru Commands
For each of these commands, replace `<tags>` with your search tags or leave blank. Use `_` instead of spaces in individual tags. Uses spaces to separate multiple tags. 

`danbooru <tags>` - 🔞**NSFW**. Returns a random image from Danbooru Donmai.  
`gelbooru <tags>` - 🔞**NSFW**. Returns a random image from Gelbooru.  
`konachan <tags>` - 🔞**NSFW**. Returns a random image from Konachan.  
`realbooru <tags>` - 🔞**NSFW**. Returns a random image from Realbooru.  
`r34 <tags>` - 🔞**NSFW**. Returns a random image from Rule34.  
`safebooru <tags>` - Returns a random image from Safebooru.  
`sakugabooru <tags>` - Returns a random image from Sakugabooru.    
`sankakucomplex <tags>` - 🔞**NSFW**. Returns a random image from Sankaku Complex.  
`xbooru <tags>` - 🔞**NSFW**. Returns a random image from Xbooru.  
`yandere <tags>` - 🔞**NSFW**. Returns a random image from yandre.re.  

### Image Commands
For all commands with `<url>` parameter, replace it with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.    

`9gag <url>` - Apply 9gag filter to an image.
`adidas <url>` - Apply Adidas filter to an image.
`adw <url>` - Apply Admin Walk filter to an image.   
`ajit <url>` - Apply Ajit Pai filter to an image.   
`america <url>` - Apply America filter to an image.   
`analysis <url>` - Apply Kolakowski Analysis filter to an image.   
`austin <url>` - Apply Austin Evans filter to an image.   
`bernie <url>` - Apply Bernie Sanders filter to an image.   
`biden <url>` - Displays an image in a display next to President Biden.   
`binoculars <url>` - Apply Binoculars filter to an image.   
`bobross <url>` - Apply Bob Ross filter to an image.   
`cmm <text>` - Creates a Change My Mind image. Replace `<text>` with any text.  
`condom <url>` - Apply Condom filter to an image.   
`comic <text>` - Displays a specified comic. Replace `<text>` with your choice. See comics.md for supported comics. 
`deepfry <url>` - Deepfry an image.    
`image <text>` - Performs a Google image search and returns the first result. Replace `<text>` with your keywords to search.  
`nuke <url>` - Deepfry an image 10 times.   
`sonicsays <text>` - Generates a Sonic Says image. Replace `<text>` with any text.  
`waifu` - Generates a waifu using Waifu Labs.  
`webcam <text>` - Displays a specified webcam. Replace `<text>` with your choice. See [webcams.md](webcams.md) for list of default supported webcams.  

### Static Text Response Commands
These commands configure static text responses that the bot uses when a command cannot be found. There are two types of static text responses, global resposnses that apply across all servers the bot is in, and local responses that apply to a single server. The following commands configure these responses.  

`statictext getall` - Get all the text responses configured for your server as a JSON file.
`statictext getallglobal` - Get all the text responses configured globally.  
`statictext addglobal <keyword> <text>` - 🔒 **Bot Owner Only** Add a global text response. Replace `<keyword>` with the word that will trigger the response and replace `<text>` with the response to send.  
`statictext add <keyword> <text>` - Add a local text response. Replace `<keyword>` with the word that will trigger the response and replace `<text>` with the response to send.  
`statictext updateglobal <keyword> <text>` - 🔒 **Bot Owner Only** Update a existing global text response. Replace `<keyword>` with the word that will trigger the response and replace `<text>` with the response to send.  
`statictext update <keyword> <text>` - Update a existing local text response. Replace `<keyword>` with the word that will trigger the response and replace `<text>` with the response to send.  
`statictext removeglobal <keyword>` - 🔒 **Bot Owner Only** Remove a global text response. Replace `<keyword>` with the triger word to be removed.  
`statictext remove <keyword>` - Remove a local text response. Replace `<keyword>` with the triger word to be removed.  

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

### YouTube Commands
`yt <text>` - Plays audio from a YouTube video in the current voice channel you are connected to. Replace `<text>` with a valid YouTube URL or video name to search for.  
`yt-d <text>` - Gets the duration of a video. Replace `<text>` with a valid YouTube URL or video name to search for.  
`ytskip` - Skip the current video playing in the voice channel.  

