﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.Kernel;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.Date;

namespace iTextSharp.Signatures
{
    internal static class SignExtensions {

        public static String JSubstring(this String str, int beginIndex, int endIndex)
        {
            return str.Substring(beginIndex, endIndex - beginIndex);
        }

        public static void AddAll<T>(this ICollection<T> t, IEnumerable<T> newItems)
        {
            foreach (T item in newItems)
            {
                t.Add(item);
            }
        }

        public static T[] ToArray<T>(this ICollection<T> col, T[] toArray) {
            T[] r = col.ToArray();
            return r;
        }

        public static void AddAll<TKey, TValue>(this IDictionary<TKey, TValue> c, IDictionary<TKey, TValue> collectionToAdd)
        {
            foreach (KeyValuePair<TKey, TValue> pair in collectionToAdd)
            {
                c[pair.Key] = pair.Value;
            }
        }

        public static T JRemoveAt<T>(this IList<T> list, int index) {
            T value = list[index];
            list.RemoveAt(index);

            return value;
        }

        public static TValue JRemove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) {
            TValue value;
            dictionary.TryGetValue(key, out value);
            dictionary.Remove(key);

            return value;
        }

        public static int Read(this Stream stream, byte[] buffer)
        {
            return stream.Read(buffer, 0, buffer.Length);
        }

        public static void Write(this Stream stream, byte[] buffer) {
            stream.Write(buffer, 0, buffer.Length);
        }

        public static void JReset(this MemoryStream stream)
        {
            stream.Position = 0;
        }

        public static int JRead(this Stream stream, byte[] buffer, int offset, int count) {
            int result = stream.Read(buffer, offset, count);
            return result == 0 ? -1 : result;
        }

        public static long Seek(this FileStream fs, long offset)
        {
            return fs.Seek(offset, SeekOrigin.Begin);
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> col, TKey key) {
            TValue value = default(TValue);
            if (key != null) {
                col.TryGetValue(key, out value);
            }

            return value;
        }

        public static bool After(this DateTime date, DateTime when) {
            return date.CompareTo(when) > 0;
        }

        public static bool After(this DateTime date, DateTimeObject when) {
            return date.CompareTo(when.Value) > 0;
        }

        public static bool Before(this DateTime date, DateTimeObject when) {
            return date.CompareTo(when.Value) < 0;
        }

        public static void InitSign(this ISigner signer, ICipherParameters pk) {
            signer.Init(true, pk);
        }

        public static void InitVerify(this ISigner signer, AsymmetricKeyParameter publicKey) {
            signer.Init(false, publicKey);
        }

        public static void Update(this ISigner signer, byte[] data) {
            signer.BlockUpdate(data, 0, data.Length);
        }

        public static void Update(this ISigner signer, byte[] data, int offset, int count) {
            signer.BlockUpdate(data, offset, count);
        }

        public static String GetAlgorithm(this ICipherParameters cp) {
            String algorithm;
            if (cp is RsaKeyParameters)
                algorithm = "RSA";
            else if (cp is DsaKeyParameters)
                algorithm = "DSA";
            else if (cp is ECKeyParameters)
                algorithm = "ECDSA";
            else
                throw new PdfException("unknown.key.algorithm {0}").SetMessageParams(cp.ToString());

            return algorithm;
        }

        public static void Update(this IDigest dgst, byte[] input)
        {
            dgst.Update(input, 0, input.Length);
        }

        public static void Update(this IDigest dgst, byte[] input, int offset, int len)
        {
            dgst.BlockUpdate(input, offset, len);
        }

        public static byte[] Digest(this IDigest dgst)
        {
            byte[] output = new byte[dgst.GetDigestSize()];
            dgst.DoFinal(output, 0);
            return output;
        }

        public static byte[] Digest(this IDigest dgst, byte[] input)
        {
            dgst.Update(input);
            return dgst.Digest();
        }
    }
}