using System;

namespace Unknown6656
{
    /// <summary>
    /// A generic data type for boxing and unboxing <see langword="struct"/>s.
    /// </summary>
    /// <typeparam name="T">The <see langword="struct"/>'s generic type.</typeparam>
    public sealed class Box<T>
        : IEquatable<Box<T>>
        where T : struct
    {
        public T Data { get; set; }

        public bool Equals(Box<T>? other) => Data.Equals(other?.Data);

        public override bool Equals(object? obj) => obj is Box<T> box && Equals(box);

        public override string? ToString() => Data.ToString();

        public override int GetHashCode() => Data.GetHashCode();


        public static bool operator ==(Box<T>? box1, Box<T>? box2) => box1?.Equals(box2) ?? box2 is null;

        public static bool operator !=(Box<T>? box1, Box<T>? box2) => !(box1 == box2);

        public static implicit operator T(Box<T> box) => box.Data;

        public static implicit operator Box<T>(T data) => new() { Data = data };
    }
}
