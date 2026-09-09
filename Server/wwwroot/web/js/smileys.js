/* ICQ-style smileys */
const SMILEYS = {
  ':)' : '😊', ':-)' : '😊',
  ':D' : '😃', ':-D' : '😃',
  ':(' : '😞', ':-(' : '😞',
  ';)' : '😉', ';-)' : '😉',
  ':P' : '😛', ':-P' : '😛', ':p' : '😛',
  '8)' : '😎', '8-)' : '😎',
  ":'(" : '😢',
  ':*' : '😘', ':-*' : '😘',
  ':O' : '😮', ':-O' : '😮',
  ':/' : '😕', ':-/' : '😕',
  '<3' : '❤️',
  '*BANG*' : '🤦‍♂️', '*bang*' : '🤦‍♂️', 'BANG' : '🤦‍♂️',
  ':beer:' : '🍺', ':coffee:' : '☕',
  ':thumbsup:' : '👍', ':thumbsdown:' : '👎',
  ':fire:' : '🔥', ':100:' : '💯', ':ok:' : '👌',
  ':wave:' : '👋', ':clap:' : '👏',
  ':heart:' : '❤️', ':broken_heart:' : '💔',
  ':smile:' : '😄', ':laugh:' : '😂', ':wink:' : '😉',
  ':cool:' : '😎', ':angry:' : '😠', ':cry:' : '😢',
  ':sad:' : '😔', ':surprised:' : '😲', ':thinking:' : '🤔',
  ':shrug:' : '🤷', ':facepalm:' : '🤦‍♂️'
};

function expandSmileys(text) {
  if (!text) return text;
  let result = text;
  const keys = Object.keys(SMILEYS).sort((a, b) => b.length - a.length);
  for (const k of keys) {
    const re = new RegExp(k.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi');
    result = result.replace(re, SMILEYS[k]);
  }
  return result;
}

function getSmileyPack() {
  return Object.entries(SMILEYS).map(([code, emoji]) => ({ code, emoji }));
}
