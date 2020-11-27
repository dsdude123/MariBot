# Commands List

This is the list of currently supported commands by MariBot. All commands should start with the configured prefix followed by the command and any parameters. Example: `z info`

### Basic and Unsorted Commands 
`info` - Displays bot information text.  
`flipcoin` - Flips a coin.  
`google <text>` - Performs a Google search. Replace `<text>` with your keywords to search.  
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

### fAPI Commands
⏲ These commands are subject to daily rate limits defined by dreadful.tech. **These commands are powered by a third-party and are provided AS-IS**. The following fAPI endpoints are unsupported or undocumented: `faceapp faceswap imagetagparser magikscript perfection proxy quote urbandictionary wikihow`

`4chan` - 🔞**NSFW**. Gets a 4Chan thread, random thread from board, or random board and thread. To specify a specific board or thread, use `4chan <path>` where path is in the form `board/thread`. For example `4chan vp` or `4chan vp/12345`.  
`9gag <url>` - Apply 9gag filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`adidas <url>` - Apply Adidas filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
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
`consent <text>` - Apply Consent filter to an image. Replace `<text>` with any text. The last posted image in the channel will be used.  
`coolguy <url>` - Apply coolguy filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`days <text>` - Creates a Days Since image. Replace `<text>` with any text.  
`deepfry <amount>` - Deepfry an image. Replace `<amount>` with any value from 1 to 100 or leave blank to use the default value. The last posted image in the channel will be used.  
`depression <url>` - Apply depression filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`disabled <url>` - Apply disabled filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`dork <url>` - Apply dork filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`duckduckgo <text>` - Searches DuckDuckGo. Replace `<text>` with any text.  
`duckduckgoimages <text>` - Searches for an image on DuckDuckGo. Replace `<text>` with any text.  
`edges <url>` - Apply edges filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`e2e <url>` - Apply Edges2Emojis filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`e2p <url>` - 🔞**NSFW**. Apply Edges2Porn filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`e2pg <url>` - 🔞**NSFW**. Apply Edges2Porn GIF filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`emojify <emoji> <emoji> <boolean> <text>` - Searches DuckDuckGo. Replace `<emoji>` with emojis, `<boolean>` with true or false whether you want the response to be verticial or not, and `<text>` with any text.  
`emojimossaic <gridsize>` - Convert image to emojis. Replace `<gridsize>` with the size of the grid of emojis or leave blank to use the default size. The last posted image in the channel will be used.  
`evalmagik <text>` - Run ImageMagik command. Replace `<text>` with your ImageMagik parameters. The last posted image in the channel will be used.  
`excuse <url>` - Apply excuse filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`eyes <eyetype>` - Replace eyes in an image. Replace `<eyetype>` with one one the following `big black blood blue googly green horror illuminati money normal pink red small spinner spongebob white yellow lucille`.   
`facedetection <url>` - Detects faces. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.  
`facemagik <text> <option>` - Apply a filter to a face. Replace `<text>` with one of `rain gold swirl frost blue charcoal tehi pixelate spin magika implode explode circle pseudocolor kaleidoscope toon ripples e2p emoji magik`. Replace `<option>` with 1 for eyes, 2 for mouth, 3 for eyes and mouth, or leave blank to apply to entire face. The last posted image in the channel will be used.  
`faceoverlay <url> <url>` - Overlays faces. Replace each `<url>` with an image URL or mention a user to use their avatar.  
`gaben <url>` - Apply Gaben filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`gay <url>` - Apply gay filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`glitch <iterations> <amount>` - Glitch an image. Replace `<iterations>` and `<amount>` with number to use or leave blank to use default values. The last posted image in the channel will be used.  
`glow <amount>` - Apply glow filter to an image. Replace `<amount>` with number to use or leave blank to use default values. The last posted image in the channel will be used.  
`god <url>` - Apply God filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`goldstar <url>` - Apply gold star filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`grill <text>` - Generate grill image. Replace `<text>` with any text.   
`hacker <template> <text>` - Generate grill image. Replace `<template>` with the template number and `<text>` with any text.  
`hawking <url>` - Apply Stephen Hawking filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`hypercam <url>` - Apply HyperCam filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`idubbbz <url>` - Apply iDubbbz filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`ifunny <url>` - Apply iFunny filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`isis <url>` - Apply ISIS filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`israel <url>` - Apply Israel filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`jack <url>` - Apply Jack filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`jackoff <url>` - Apply jackoff filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`keemstar <url>` - Apply Keemstar filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`keemstar2 <url>` - Apply a different Keemstar filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`kekistan <url>` - Apply Kekistan filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`kirby <url>` - Apply Kirby filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`linus <url>` - Apply Linus filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`logan <url>` - Apply Logan filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`logout <text>` - Generate logout image. Replace `<text>` with any text.   
`memorial <text>` - Generate memorial image. Replace `<text>` with any text.   
`miranda <url>` - Apply Miranda filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`mistake <url>` - Apply mistake filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`nooseguy <url>` - Apply Noose Guy filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`northkorea <url>` - Apply North Korea filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`oldguy <url>` - Apply Old Guy filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`owo <url>` - Apply owo filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`pistol <url>` - Apply pistol filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`pixelate <amount>` - Apply pixelate filter to an image. Replace `<amount>` with number to use or leave blank to use default values. The last posted image in the channel will be used.  
`pne <option>` - Apply glow filter to an image. Replace `<option>` with 2 to invert, 3 to composite, 4 to invert and composite, or leave blank to use default values. The last posted image in the channel will be used.  
`pornhub <text>` - Generate PornHub image. Replace `<text>` with any text. The last posted image in the channel will be used.  
`portal <url>` - Apply Portal filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`presidential <text>` - Generate presidential image. Replace `<text>` with any text.  
`racecard <text>` - Generate racecard image. Replace `<text>` with any text.  
`realfact <text>` - Generate realfact image. Replace `<text>` with any text.  
`recaptcha <text>` - Generate recaptcha image. Replace `<text>` with any text.  
`fapi_reminder <text>` - Generate reminder image. Replace `<text>` with any text. The last posted image in the channel will be used.  
`resize <url>` - Apply resize filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`respects <url>` - Apply respects filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`retro <text>` - Generate retro image. Replace `<text>` with any text. The last posted image in the channel will be used.  
`rextester <language> <code>` - Execute Rextester. Replace `<language>` with programming language and `<code>` with code to run.  
`rtx <url> <url>` - Generates RTX image. Replace each `<url>` with an image URL or mention a user to use their avatar.  
`russia <url>` - Apply Russia filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`screenshot <url> <wait>` - Screenshots a webpage. Replace `<url>` with an URL and replace `<wait>` with time to wait in ms before taking the screenshot or leave blank to use default wait.   
`shit <url>` - Apply shit filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`shooting <text>` - Generate shooting image. Replace `<text>` with any text.  
`shotgun <url>` - Apply shotgun filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`simpsondisabled <text>` - Generate Simpson disabled image. Replace `<text>` with any text.  
`smg <url>` - Apply SMG filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`snapchat <filter> <snow>` - Apply SnapChat filter to an image. Replace `<filter>` with one of `angery bambi dog dog2 bunny cat cat2 christmas heart joy flowers flowers2 devil glasses moustache notsobot autism sunglasses squidward thug thinking thanos` and replace `<snow>` with true or false to apply snow effect (only on `christmas` filter). The last posted image in the channel will be used.  
`sonic <text>` - Generate Sonic image. Replace `<text>` with any text.  
`spain <url>` - Apply Spain filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`starman <url>` - Apply Starman filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`steamplaying <game>` - Get Steam playing statistics. Replace `<game>` with game to get info for.   
`stock <url>` - Apply stock filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`supreme <url>` - Apply Supreme filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`thinking <level>` - Apply thinking filter to an image. Replace `<level>` with number to use or leave blank to use default values. The last posted image in the channel will be used.  
`thonkify <text>` - Generate thonkify image. Replace `<text>` with any text.  
`trans <url>` - Apply trans filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`trump <url>` - Apply Trump filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`ugly <url>` - Apply ugly filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`uk <url>` - Apply UK filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`urlify <url>` - URLify a link. Replace `<url>` with an URL.
`ussr <url>` - Apply USSR filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`vending <url>` - Apply vending filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`watchmojo <text>` - Generate WatchMojo image. Replace `<text>` with any text. The last posted image in the channel will be used.  
`wheeze <url>` - Apply wheeze filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`wonka <text>` - Generate Wonka image. Replace `<text>` with any text.  
`wth <url>` - Apply WTH filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`yusuke <url>` - Apply Yusuke filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`zoom <url>` - Apply zoom filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   
`zuckerberg <url>` - Apply Zuckerberg filter to an image. Replace `<url>` with an image URL, mention a user to use their avatar, or leave blank to use the last posted image in the channel.   

### Image Commands
`comic <text>` - Displays a specified comic. Replace `<text>` with your choice. See comics.md for supported comics.  
`image <text>` - Performs a Google image search and returns the first result. Replace `<text>` with your keywords to search.  
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

### Game Commands
Append all commands in this category with `game`. For example, `game <command>` where `<command>` is a command from this category.

`balance` - Display your credit balance.  
`daily` - Get a daily credit bonus.  

### YouTube Commands
`yt <text>` - Plays audio from a YouTube video in the current voice channel you are connected to. Replace `<text>` with a valid YouTube URL or video name to search for.  
`yt-d <text>` - Gets the duration of a video. Replace `<text>` with a valid YouTube URL or video name to search for.  
`ytskip` - Skip the current video playing in the voice channel.  

