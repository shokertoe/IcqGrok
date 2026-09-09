/** Classic ICQ emoticon pack (including headbanging *BANG*) */
window.ICQ_SMILEYS = {
  ":)": "🙂", ":-)": "🙂",
  ":(": "🙁", ":-(": "🙁",
  ":D": "😀", ":-D": "😀",
  ";)": "😉", ";-)": "😉",
  ":P": "😛", ":-P": "😛", ":p": "😛",
  ":*": "😘", ":-*": "😘",
  "8)": "😎", "8-)": "😎", "B)": "😎",
  ":O": "😮", ":-O": "😮", ":o": "😮",
  ":|": "😐", ":-|": "😐",
  ":$": "😳", ":-$": "😳",
  ":@": "😠", ":-@": "😠",
  ":'(": "😢",
  "XD": "😆", "xD": "😆",
  ":/": "😕", ":-/": "😕",
  "<3": "❤️", "</3": "💔",
  ":3": "😺",
  "O:)": "😇", "O:-)": "😇",
  ">:(": "😡", ">:-(": "😡",
  ":X": "🤐", ":-X": "🤐",
  "*JOKINGLY*": "😜",
  "*KISSING*": "💋",
  "*STOP*": "🛑",
  "*THUMBS UP*": "👍",
  "*THUMBS DOWN*": "👎",
  "*APPLAUD*": "👏",
  "*OK*": "👌",
  "*HELP*": "🆘",
  "*PARTY*": "🥳",
  "*DRINK*": "🍺",
  "*COFFEE*": "☕",
  "*ROSE*": "🌹",
  "*SUN*": "☀️",
  "*RAIN*": "🌧️",
  "*MUSIC*": "🎵",
  "*DANCE*": "💃",
  "*ANGEL*": "😇",
  "*DEVIL*": "😈",
  "*LOVE*": "🥰",
  "*CRAZY*": "🤪",
  "*SICK*": "🤒",
  "*YAWN*": "🥱",
  "*SLEEP*": "😴",
  "*THINK*": "🤔",
  "*IDEA*": "💡",
  "*BOMB*": "💣",
  "*FIRE*": "🔥",
  // Legendary ICQ headbang against the wall
  "*BANG*": "🤕🧱",
  "*HEADBANG*": "🤕🧱",
  ":bang:": "🤕🧱",
  "*WALL*": "🤕🧱",
  "*FACEPALM*": "🤦",
  "*PANIC*": "😱",
  "*SIGH*": "😮‍💨",
  "*SHRUG*": "🤷"
};

window.expandSmileys = function (text) {
  if (!text) return "";
  let result = String(text);
  const entries = Object.entries(window.ICQ_SMILEYS).sort((a, b) => b[0].length - a[0].length);
  for (const [code, emoji] of entries) {
    const re = new RegExp(code.replace(/[.*+?^${}()|[\]\\]/g, "\\$&"), "gi");
    result = result.replace(re, emoji);
  }
  return result;
};

/** Picker rows for UI */
window.SMILEY_PICKER = [
  [":)", ";)", ":D", ":P", ":*", "8)", ":O", ":("],
  [":'(", ":@", ":$", ":|", ":/", "<3", "O:)", ">:("],
  ["*BANG*", "*LOVE*", "*PARTY*", "*THUMBS UP*", "*APPLAUD*", "*FIRE*", "*COFFEE*", "*MUSIC*"],
  ["*HEADBANG*", "*SHRUG*", "*PANIC*", "*SLEEP*", "*THINK*", "*DEVIL*", "*ANGEL*", "*ROSE*"]
];
