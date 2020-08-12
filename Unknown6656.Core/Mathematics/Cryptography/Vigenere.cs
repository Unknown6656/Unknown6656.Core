using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Unknown6656.Mathematics.Cryptography
{
    public sealed class Vigenere
         : StringCipher
    {
        private readonly string _alphabet;

        public char[] Alphabet => _alphabet.ToArray();


        public Vigenere(params char[] alphabet)
        {
            _alphabet = new string(alphabet);
        }

        public Vigenere(IEnumerable<char> alphabet)
            : this (alphabet.ToArray())
        {
        }

        private string _crypt(string input, string key, Func<int, int, int> op)
        {
            char[] output = new char[input.Length];

            Parallel.For(0, input.Length, i => output[i] = _alphabet.IndexOf(input[i]) switch
            {
                -1 => input[i],
                int c => _alphabet[(op(c, _alphabet.IndexOf(key[i % key.Length])) + _alphabet.Length) % _alphabet.Length]
            });

            return new string(output);
        }

        public override string Encrypt(string key, string message) => _crypt(message, key, (x, y) => x + y);

        public override string Decrypt(string key, string cipher) => _crypt(cipher, key, (x, y) => x - y);

        public (string password, double probability)[] Crack(string cipher, IDictionary<char, double> char_frequency, int max_passw_length)
        {
            IEnumerable<(char Char, int CharIndex, double Probability)> CreateDistr(IEnumerable<char> s)
            {
                IEnumerable<(char Char, int Count)> abs = from c in s
                                                          where _alphabet.Contains(c)
                                                          group c by c into g
                                                          let count = g.Count()
                                                          orderby count descending
                                                          select (g.Key, g.Count());
                double sum = abs.Sum(d => (double)d.Count);

                return abs.Select(t => (t.Char, _alphabet.IndexOf(t.Char), t.Count / sum));
            }

            var distribution_reference = from c in char_frequency.Keys
                                         let prob = char_frequency[c] / char_frequency.Values.Sum()
                                         orderby prob descending
                                         select new
                                         {
                                             Char = c,
                                             CharIndex = _alphabet.IndexOf(c),
                                             Probability = prob
                                         };

            return (from length in Enumerable.Range(1, Math.Min(cipher.Length - 1, max_passw_length))
                    let password_chars = from index in Enumerable.Range(0, length)
                                         let distr = CreateDistr(cipher.Where((c, i) => (i - index) % length == 0))
                                         let shift = (distr.First().CharIndex - distribution_reference.First().CharIndex + _alphabet.Length) % _alphabet.Length
                                         select _alphabet[shift]
                    let password = new string(password_chars.ToArray())
                    let decrypted = Decrypt(password, cipher)
                    let distr = CreateDistr(decrypted)
                    let delta = from d in distr
                                from r in distribution_reference
                                where d.Char == r.Char
                                let diff = d.Probability - r.Probability
                                select Math.Sqrt(diff * diff)
                    let probability = 1 - delta.Sum()
                    group password by probability into equivalents
                    let shortest = equivalents.OrderBy(pass => pass.Length).First()
                    orderby equivalents.Key descending
                    select (shortest, equivalents.Key)).ToArray();
        }
    }
}
