﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Physics.Optics;
using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics;
using Unknown6656.Generics;

namespace Unknown6656.Imaging;


public abstract class ColorMap
{
    #region STATIC COLOR MAPS

    public static DiscreteColorMap LegacyBlackbodyHeat { get; } = Uniform(0xf000, 0xff88, 0xffb7, 0xffff, 0xf9ef, 0xf6cf);

    public static DiscreteColorMap NarrowBlackbodyHeat { get; } = new(Enumerable.Range(0, 1000).Select(i => RGBAColor.FromBlackbodyRadiation(7 * i)));

    public static DiscreteColorMap ExtendedBlackbodyHeat { get; } = new(Enumerable.Range(0, 1000).Select(i => RGBAColor.FromBlackbodyRadiation(14 * i)));

    public static DiscreteColorMap BlackbodyHeat { get; } = new(Enumerable.Range(0, 1000).Select(i => RGBAColor.FromBlackbodyRadiation(11 * i)));

    public static ContinuousColorMap HueMap { get; } = new(s => RGBAColor.FromHSL(s / Scalar.Tau, 1, 1));

    public static ContinuousColorMap VisibleSpectrum { get; } = new(s => new Wavelength(Wavelength.HighestVisibleWavelength.InNanometers - (s.Clamp() * (Wavelength.HighestVisibleWavelength - Wavelength.LowestVisibleWavelength).InNanometers)).ToColor());

    public static DiscreteColorMap Terrain { get; } = new DiscreteColorMap(
        (0, (.2, .2, .6)),
        (.15, (0, .6, 1)),
        (.25, (0, .8, .4)),
        (.50, (1, 1, .6)),
        (.75, (.5, .36, .33)),
        (1, (1, 1, 1))
    );

    public static DiscreteColorMap Blues { get; } = Uniform(
        (0.96862745098039216, 0.98431372549019602, 1.0),
        (0.87058823529411766, 0.92156862745098034, 0.96862745098039216),
        (0.77647058823529413, 0.85882352941176465, 0.93725490196078431),
        (0.61960784313725492, 0.792156862745098, 0.88235294117647056),
        (0.41960784313725491, 0.68235294117647061, 0.83921568627450982),
        (0.25882352941176473, 0.5725490196078431, 0.77647058823529413),
        (0.12941176470588237, 0.44313725490196076, 0.70980392156862748),
        (0.03137254901960784, 0.31764705882352939, 0.61176470588235299),
        (0.03137254901960784, 0.18823529411764706, 0.41960784313725491)
    );

    public static DiscreteColorMap BrBG { get; } = Uniform(
        (0.32941176470588235, 0.18823529411764706, 0.0196078431372549),
        (0.5490196078431373, 0.31764705882352939, 0.0392156862745098),
        (0.74901960784313726, 0.50588235294117645, 0.17647058823529413),
        (0.87450980392156863, 0.76078431372549016, 0.49019607843137253),
        (0.96470588235294119, 0.90980392156862744, 0.76470588235294112),
        (0.96078431372549022, 0.96078431372549022, 0.96078431372549022),
        (0.7803921568627451, 0.91764705882352937, 0.89803921568627454),
        (0.50196078431372548, 0.80392156862745101, 0.75686274509803919),
        (0.20784313725490197, 0.59215686274509804, 0.5607843137254902),
        (0.00392156862745098, 0.4, 0.36862745098039218),
        (0.0, 0.23529411764705882, 0.18823529411764706)
    );

    public static DiscreteColorMap BuGn { get; } = Uniform(
        (0.96862745098039216, 0.9882352941176471, 0.99215686274509807),
        (0.89803921568627454, 0.96078431372549022, 0.97647058823529409),
        (0.8, 0.92549019607843142, 0.90196078431372551),
        (0.6, 0.84705882352941175, 0.78823529411764703),
        (0.4, 0.76078431372549016, 0.64313725490196083),
        (0.25490196078431371, 0.68235294117647061, 0.46274509803921571),
        (0.13725490196078433, 0.54509803921568623, 0.27058823529411763),
        (0.0, 0.42745098039215684, 0.17254901960784313),
        (0.0, 0.26666666666666666, 0.10588235294117647)
    );

    public static DiscreteColorMap BuPu { get; } = Uniform(
        (0.96862745098039216, 0.9882352941176471, 0.99215686274509807),
        (0.8784313725490196, 0.92549019607843142, 0.95686274509803926),
        (0.74901960784313726, 0.82745098039215681, 0.90196078431372551),
        (0.61960784313725492, 0.73725490196078436, 0.85490196078431369),
        (0.5490196078431373, 0.58823529411764708, 0.77647058823529413),
        (0.5490196078431373, 0.41960784313725491, 0.69411764705882351),
        (0.53333333333333333, 0.25490196078431371, 0.61568627450980395),
        (0.50588235294117645, 0.05882352941176471, 0.48627450980392156),
        (0.30196078431372547, 0.0, 0.29411764705882354)
    );

    public static DiscreteColorMap GnBu { get; } = Uniform(
        (0.96862745098039216, 0.9882352941176471, 0.94117647058823528),
        (0.8784313725490196, 0.95294117647058818, 0.85882352941176465),
        (0.8, 0.92156862745098034, 0.77254901960784317),
        (0.6588235294117647, 0.8666666666666667, 0.70980392156862748),
        (0.4823529411764706, 0.8, 0.7686274509803922),
        (0.30588235294117649, 0.70196078431372544, 0.82745098039215681),
        (0.16862745098039217, 0.5490196078431373, 0.74509803921568629),
        (0.03137254901960784, 0.40784313725490196, 0.67450980392156867),
        (0.03137254901960784, 0.25098039215686274, 0.50588235294117645)
    );

    public static DiscreteColorMap Greens { get; } = Uniform(
        (0.96862745098039216, 0.9882352941176471, 0.96078431372549022),
        (0.89803921568627454, 0.96078431372549022, 0.8784313725490196),
        (0.7803921568627451, 0.9137254901960784, 0.75294117647058822),
        (0.63137254901960782, 0.85098039215686272, 0.60784313725490191),
        (0.45490196078431372, 0.7686274509803922, 0.46274509803921571),
        (0.25490196078431371, 0.6705882352941176, 0.36470588235294116),
        (0.13725490196078433, 0.54509803921568623, 0.27058823529411763),
        (0.0, 0.42745098039215684, 0.17254901960784313),
        (0.0, 0.26666666666666666, 0.10588235294117647)
    );

    public static DiscreteColorMap Grays { get; } = Uniform(
        (1.0, 1.0, 1.0),
        (0.94117647058823528, 0.94117647058823528, 0.94117647058823528),
        (0.85098039215686272, 0.85098039215686272, 0.85098039215686272),
        (0.74117647058823533, 0.74117647058823533, 0.74117647058823533),
        (0.58823529411764708, 0.58823529411764708, 0.58823529411764708),
        (0.45098039215686275, 0.45098039215686275, 0.45098039215686275),
        (0.32156862745098042, 0.32156862745098042, 0.32156862745098042),
        (0.14509803921568629, 0.14509803921568629, 0.14509803921568629),
        (0.0, 0.0, 0.0)
    );

    public static DiscreteColorMap Oranges { get; } = Uniform(
        (1.0, 0.96078431372549022, 0.92156862745098034),
        (0.99607843137254903, 0.90196078431372551, 0.80784313725490198),
        (0.99215686274509807, 0.81568627450980391, 0.63529411764705879),
        (0.99215686274509807, 0.68235294117647061, 0.41960784313725491),
        (0.99215686274509807, 0.55294117647058827, 0.23529411764705882),
        (0.94509803921568625, 0.41176470588235292, 0.07450980392156863),
        (0.85098039215686272, 0.28235294117647058, 0.00392156862745098),
        (0.65098039215686276, 0.21176470588235294, 0.01176470588235294),
        (0.49803921568627452, 0.15294117647058825, 0.01568627450980392)
    );

    public static DiscreteColorMap OrRd { get; } = Uniform(
        (1.0, 0.96862745098039216, 0.92549019607843142),
        (0.99607843137254903, 0.90980392156862744, 0.78431372549019607),
        (0.99215686274509807, 0.83137254901960789, 0.61960784313725492),
        (0.99215686274509807, 0.73333333333333328, 0.51764705882352946),
        (0.9882352941176471, 0.55294117647058827, 0.34901960784313724),
        (0.93725490196078431, 0.396078431372549, 0.28235294117647058),
        (0.84313725490196079, 0.18823529411764706, 0.12156862745098039),
        (0.70196078431372544, 0.0, 0.0),
        (0.49803921568627452, 0.0, 0.0)
    );

    public static DiscreteColorMap PiYG { get; } = Uniform(
        (0.55686274509803924, 0.00392156862745098, 0.32156862745098042),
        (0.77254901960784317, 0.10588235294117647, 0.49019607843137253),
        (0.87058823529411766, 0.46666666666666667, 0.68235294117647061),
        (0.94509803921568625, 0.71372549019607845, 0.85490196078431369),
        (0.99215686274509807, 0.8784313725490196, 0.93725490196078431),
        (0.96862745098039216, 0.96862745098039216, 0.96862745098039216),
        (0.90196078431372551, 0.96078431372549022, 0.81568627450980391),
        (0.72156862745098038, 0.88235294117647056, 0.52549019607843139),
        (0.49803921568627452, 0.73725490196078436, 0.25490196078431371),
        (0.30196078431372547, 0.5725490196078431, 0.12941176470588237),
        (0.15294117647058825, 0.39215686274509803, 0.09803921568627451)
    );

    public static DiscreteColorMap PrGN { get; } = Uniform(
        (0.25098039215686274, 0.0, 0.29411764705882354),
        (0.46274509803921571, 0.16470588235294117, 0.51372549019607838),
        (0.6, 0.4392156862745098, 0.6705882352941176),
        (0.76078431372549016, 0.6470588235294118, 0.81176470588235294),
        (0.90588235294117647, 0.83137254901960789, 0.90980392156862744),
        (0.96862745098039216, 0.96862745098039216, 0.96862745098039216),
        (0.85098039215686272, 0.94117647058823528, 0.82745098039215681),
        (0.65098039215686276, 0.85882352941176465, 0.62745098039215685),
        (0.35294117647058826, 0.68235294117647061, 0.38039215686274508),
        (0.10588235294117647, 0.47058823529411764, 0.21568627450980393),
        (0.0, 0.26666666666666666, 0.10588235294117647)
    );

    public static DiscreteColorMap PuBu { get; } = Uniform(
        (1.0, 0.96862745098039216, 0.98431372549019602),
        (0.92549019607843142, 0.90588235294117647, 0.94901960784313721),
        (0.81568627450980391, 0.81960784313725488, 0.90196078431372551),
        (0.65098039215686276, 0.74117647058823533, 0.85882352941176465),
        (0.45490196078431372, 0.66274509803921566, 0.81176470588235294),
        (0.21176470588235294, 0.56470588235294117, 0.75294117647058822),
        (0.0196078431372549, 0.4392156862745098, 0.69019607843137254),
        (0.01568627450980392, 0.35294117647058826, 0.55294117647058827),
        (0.00784313725490196, 0.2196078431372549, 0.34509803921568627)
    );

    public static DiscreteColorMap PuBuGn { get; } = Uniform(
        (1.0, 0.96862745098039216, 0.98431372549019602),
        (0.92549019607843142, 0.88627450980392153, 0.94117647058823528),
        (0.81568627450980391, 0.81960784313725488, 0.90196078431372551),
        (0.65098039215686276, 0.74117647058823533, 0.85882352941176465),
        (0.40392156862745099, 0.66274509803921566, 0.81176470588235294),
        (0.21176470588235294, 0.56470588235294117, 0.75294117647058822),
        (0.00784313725490196, 0.50588235294117645, 0.54117647058823526),
        (0.00392156862745098, 0.42352941176470588, 0.34901960784313724),
        (0.00392156862745098, 0.27450980392156865, 0.21176470588235294)
    );

    public static DiscreteColorMap PuOr { get; } = Uniform(
        (0.49803921568627452, 0.23137254901960785, 0.03137254901960784),
        (0.70196078431372544, 0.34509803921568627, 0.02352941176470588),
        (0.8784313725490196, 0.50980392156862742, 0.07843137254901961),
        (0.99215686274509807, 0.72156862745098038, 0.38823529411764707),
        (0.99607843137254903, 0.8784313725490196, 0.71372549019607845),
        (0.96862745098039216, 0.96862745098039216, 0.96862745098039216),
        (0.84705882352941175, 0.85490196078431369, 0.92156862745098034),
        (0.69803921568627447, 0.6705882352941176, 0.82352941176470584),
        (0.50196078431372548, 0.45098039215686275, 0.67450980392156867),
        (0.32941176470588235, 0.15294117647058825, 0.53333333333333333),
        (0.17647058823529413, 0.0, 0.29411764705882354)
    );

    public static DiscreteColorMap PuRd { get; } = Uniform(
        (0.96862745098039216, 0.95686274509803926, 0.97647058823529409),
        (0.90588235294117647, 0.88235294117647056, 0.93725490196078431),
        (0.83137254901960789, 0.72549019607843135, 0.85490196078431369),
        (0.78823529411764703, 0.58039215686274515, 0.7803921568627451),
        (0.87450980392156863, 0.396078431372549, 0.69019607843137254),
        (0.90588235294117647, 0.16078431372549021, 0.54117647058823526),
        (0.80784313725490198, 0.07058823529411765, 0.33725490196078434),
        (0.59607843137254901, 0.0, 0.2627450980392157),
        (0.40392156862745099, 0.0, 0.12156862745098039)
    );

    public static DiscreteColorMap Purples { get; } = Uniform(
        (0.9882352941176471, 0.98431372549019602, 0.99215686274509807),
        (0.93725490196078431, 0.92941176470588238, 0.96078431372549022),
        (0.85490196078431369, 0.85490196078431369, 0.92156862745098034),
        (0.73725490196078436, 0.74117647058823533, 0.86274509803921573),
        (0.61960784313725492, 0.60392156862745094, 0.78431372549019607),
        (0.50196078431372548, 0.49019607843137253, 0.72941176470588232),
        (0.41568627450980394, 0.31764705882352939, 0.63921568627450975),
        (0.32941176470588235, 0.15294117647058825, 0.5607843137254902),
        (0.24705882352941178, 0.0, 0.49019607843137253)
    );

    public static DiscreteColorMap RdBu { get; } = Uniform(
        (0.40392156862745099, 0.0, 0.12156862745098039),
        (0.69803921568627447, 0.09411764705882353, 0.16862745098039217),
        (0.83921568627450982, 0.37647058823529411, 0.30196078431372547),
        (0.95686274509803926, 0.6470588235294118, 0.50980392156862742),
        (0.99215686274509807, 0.85882352941176465, 0.7803921568627451),
        (0.96862745098039216, 0.96862745098039216, 0.96862745098039216),
        (0.81960784313725488, 0.89803921568627454, 0.94117647058823528),
        (0.5725490196078431, 0.77254901960784317, 0.87058823529411766),
        (0.2627450980392157, 0.57647058823529407, 0.76470588235294112),
        (0.12941176470588237, 0.4, 0.67450980392156867),
        (0.0196078431372549, 0.18823529411764706, 0.38039215686274508)
    );

    public static DiscreteColorMap RdGy { get; } = Uniform(
        (0.40392156862745099, 0.0, 0.12156862745098039),
        (0.69803921568627447, 0.09411764705882353, 0.16862745098039217),
        (0.83921568627450982, 0.37647058823529411, 0.30196078431372547),
        (0.95686274509803926, 0.6470588235294118, 0.50980392156862742),
        (0.99215686274509807, 0.85882352941176465, 0.7803921568627451),
        (1.0, 1.0, 1.0),
        (0.8784313725490196, 0.8784313725490196, 0.8784313725490196),
        (0.72941176470588232, 0.72941176470588232, 0.72941176470588232),
        (0.52941176470588236, 0.52941176470588236, 0.52941176470588236),
        (0.30196078431372547, 0.30196078431372547, 0.30196078431372547),
        (0.10196078431372549, 0.10196078431372549, 0.10196078431372549)
    );

    public static DiscreteColorMap RdPu { get; } = Uniform(
        (1.0, 0.96862745098039216, 0.95294117647058818),
        (0.99215686274509807, 0.8784313725490196, 0.86666666666666667),
        (0.9882352941176471, 0.77254901960784317, 0.75294117647058822),
        (0.98039215686274506, 0.62352941176470589, 0.70980392156862748),
        (0.96862745098039216, 0.40784313725490196, 0.63137254901960782),
        (0.86666666666666667, 0.20392156862745098, 0.59215686274509804),
        (0.68235294117647061, 0.00392156862745098, 0.49411764705882355),
        (0.47843137254901963, 0.00392156862745098, 0.46666666666666667),
        (0.28627450980392155, 0.0, 0.41568627450980394)
    );

    public static DiscreteColorMap PdYlBu { get; } = Uniform(
        (0.6470588235294118, 0.0, 0.14901960784313725),
        (0.84313725490196079, 0.18823529411764706, 0.15294117647058825),
        (0.95686274509803926, 0.42745098039215684, 0.2627450980392157),
        (0.99215686274509807, 0.68235294117647061, 0.38039215686274508),
        (0.99607843137254903, 0.8784313725490196, 0.56470588235294117),
        (1.0, 1.0, 0.74901960784313726),
        (0.8784313725490196, 0.95294117647058818, 0.97254901960784312),
        (0.6705882352941176, 0.85098039215686272, 0.9137254901960784),
        (0.45490196078431372, 0.67843137254901964, 0.81960784313725488),
        (0.27058823529411763, 0.45882352941176469, 0.70588235294117652),
        (0.19215686274509805, 0.21176470588235294, 0.58431372549019611)
    );

    public static DiscreteColorMap RdYlGn { get; } = Uniform(
        (0.6470588235294118, 0.0, 0.14901960784313725),
        (0.84313725490196079, 0.18823529411764706, 0.15294117647058825),
        (0.95686274509803926, 0.42745098039215684, 0.2627450980392157),
        (0.99215686274509807, 0.68235294117647061, 0.38039215686274508),
        (0.99607843137254903, 0.8784313725490196, 0.54509803921568623),
        (1.0, 1.0, 0.74901960784313726),
        (0.85098039215686272, 0.93725490196078431, 0.54509803921568623),
        (0.65098039215686276, 0.85098039215686272, 0.41568627450980394),
        (0.4, 0.74117647058823533, 0.38823529411764707),
        (0.10196078431372549, 0.59607843137254901, 0.31372549019607843),
        (0.0, 0.40784313725490196, 0.21568627450980393)
    );

    public static DiscreteColorMap Reds { get; } = Uniform(
        (1.0, 0.96078431372549022, 0.94117647058823528),
        (0.99607843137254903, 0.8784313725490196, 0.82352941176470584),
        (0.9882352941176471, 0.73333333333333328, 0.63137254901960782),
        (0.9882352941176471, 0.5725490196078431, 0.44705882352941179),
        (0.98431372549019602, 0.41568627450980394, 0.29019607843137257),
        (0.93725490196078431, 0.23137254901960785, 0.17254901960784313),
        (0.79607843137254897, 0.094117647058823528, 0.11372549019607843),
        (0.6470588235294118, 0.058823529411764705, 0.08235294117647058),
        (0.40392156862745099, 0.0, 0.05098039215686274)
    );

    public static DiscreteColorMap Spectral { get; } = Uniform(
        (0.61960784313725492, 0.003921568627450980, 0.25882352941176473),
        (0.83529411764705885, 0.24313725490196078, 0.30980392156862746),
        (0.95686274509803926, 0.42745098039215684, 0.2627450980392157),
        (0.99215686274509807, 0.68235294117647061, 0.38039215686274508),
        (0.99607843137254903, 0.8784313725490196, 0.54509803921568623),
        (1.0, 1.0, 0.74901960784313726),
        (0.90196078431372551, 0.96078431372549022, 0.59607843137254901),
        (0.6705882352941176, 0.8666666666666667, 0.64313725490196083),
        (0.4, 0.76078431372549016, 0.6470588235294118),
        (0.19607843137254902, 0.53333333333333333, 0.74117647058823533),
        (0.36862745098039218, 0.30980392156862746, 0.63529411764705879)
    );

    public static DiscreteColorMap YlGn { get; } = Uniform(
        (1.0, 1.0, 0.89803921568627454),
        (0.96862745098039216, 0.9882352941176471, 0.72549019607843135),
        (0.85098039215686272, 0.94117647058823528, 0.63921568627450975),
        (0.67843137254901964, 0.8666666666666667, 0.55686274509803924),
        (0.47058823529411764, 0.77647058823529413, 0.47450980392156861),
        (0.25490196078431371, 0.6705882352941176, 0.36470588235294116),
        (0.13725490196078433, 0.51764705882352946, 0.2627450980392157),
        (0.0, 0.40784313725490196, 0.21568627450980393),
        (0.0, 0.27058823529411763, 0.16078431372549021)
    );

    public static DiscreteColorMap YlGnBu { get; } = Uniform(
        (1.0, 1.0, 0.85098039215686272),
        (0.92941176470588238, 0.97254901960784312, 0.69411764705882351),
        (0.7803921568627451, 0.9137254901960784, 0.70588235294117652),
        (0.49803921568627452, 0.80392156862745101, 0.73333333333333328),
        (0.25490196078431371, 0.71372549019607845, 0.7686274509803922),
        (0.11372549019607843, 0.56862745098039214, 0.75294117647058822),
        (0.13333333333333333, 0.36862745098039218, 0.6588235294117647),
        (0.14509803921568629, 0.20392156862745098, 0.58039215686274515),
        (0.03137254901960784, 0.11372549019607843, 0.34509803921568627)
    );

    public static DiscreteColorMap YlOrBn { get; } = Uniform(
        (1.0, 1.0, 0.89803921568627454),
        (1.0, 0.96862745098039216, 0.73725490196078436),
        (0.99607843137254903, 0.8901960784313725, 0.56862745098039214),
        (0.99607843137254903, 0.7686274509803922, 0.30980392156862746),
        (0.99607843137254903, 0.6, 0.16078431372549021),
        (0.92549019607843142, 0.4392156862745098, 0.07843137254901961),
        (0.8, 0.29803921568627451, 0.00784313725490196),
        (0.6, 0.20392156862745098, 0.01568627450980392),
        (0.4, 0.14509803921568629, 0.02352941176470588)
    );

    public static DiscreteColorMap YlOrRd { get; } = Uniform(
        (1.0, 1.0, 0.8),
        (1.0, 0.92941176470588238, 0.62745098039215685),
        (0.99607843137254903, 0.85098039215686272, 0.46274509803921571),
        (0.99607843137254903, 0.69803921568627447, 0.29803921568627451),
        (0.99215686274509807, 0.55294117647058827, 0.23529411764705882),
        (0.9882352941176471, 0.30588235294117649, 0.16470588235294117),
        (0.8901960784313725, 0.10196078431372549, 0.10980392156862745),
        (0.74117647058823533, 0.0, 0.14901960784313725),
        (0.50196078431372548, 0.0, 0.14901960784313725)
    );

    public static DiscreteColorMap Accent { get; } = Uniform(
        (0.49803921568627452, 0.78823529411764703, 0.49803921568627452),
        (0.74509803921568629, 0.68235294117647061, 0.83137254901960789),
        (0.99215686274509807, 0.75294117647058822, 0.52549019607843139),
        (1.0, 1.0, 0.6),
        (0.2196078431372549, 0.42352941176470588, 0.69019607843137254),
        (0.94117647058823528, 0.00784313725490196, 0.49803921568627452),
        (0.74901960784313726, 0.35686274509803922, 0.09019607843137254),
        (0.4, 0.4, 0.4)
    );

    public static DiscreteColorMap Dark { get; } = Uniform(
        (0.10588235294117647, 0.61960784313725492, 0.46666666666666667),
        (0.85098039215686272, 0.37254901960784315, 0.00784313725490196),
        (0.45882352941176469, 0.4392156862745098, 0.70196078431372544),
        (0.90588235294117647, 0.16078431372549021, 0.54117647058823526),
        (0.4, 0.65098039215686276, 0.11764705882352941),
        (0.90196078431372551, 0.6705882352941176, 0.00784313725490196),
        (0.65098039215686276, 0.46274509803921571, 0.11372549019607843),
        (0.4, 0.4, 0.4)
    );

    public static DiscreteColorMap Paired { get; } = Uniform(
        (0.65098039215686276, 0.80784313725490198, 0.8901960784313725),
        (0.12156862745098039, 0.47058823529411764, 0.70588235294117652),
        (0.69803921568627447, 0.87450980392156863, 0.54117647058823526),
        (0.2, 0.62745098039215685, 0.17254901960784313),
        (0.98431372549019602, 0.60392156862745094, 0.6),
        (0.8901960784313725, 0.10196078431372549, 0.10980392156862745),
        (0.99215686274509807, 0.74901960784313726, 0.43529411764705883),
        (1.0, 0.49803921568627452, 0.0),
        (0.792156862745098, 0.69803921568627447, 0.83921568627450982),
        (0.41568627450980394, 0.23921568627450981, 0.60392156862745094),
        (1.0, 1.0, 0.6),
        (0.69411764705882351, 0.34901960784313724, 0.15686274509803921)
    );

    public static DiscreteColorMap Pastel1 { get; } = Uniform(
        (0.98431372549019602, 0.70588235294117652, 0.68235294117647061),
        (0.70196078431372544, 0.80392156862745101, 0.8901960784313725),
        (0.8, 0.92156862745098034, 0.77254901960784317),
        (0.87058823529411766, 0.79607843137254897, 0.89411764705882357),
        (0.99607843137254903, 0.85098039215686272, 0.65098039215686276),
        (1.0, 1.0, 0.8),
        (0.89803921568627454, 0.84705882352941175, 0.74117647058823533),
        (0.99215686274509807, 0.85490196078431369, 0.92549019607843142),
        (0.94901960784313721, 0.94901960784313721, 0.94901960784313721)
    );

    public static DiscreteColorMap Pastel2 { get; } = Uniform(
        (0.70196078431372544, 0.88627450980392153, 0.80392156862745101),
        (0.99215686274509807, 0.80392156862745101, 0.67450980392156867),
        (0.79607843137254897, 0.83529411764705885, 0.90980392156862744),
        (0.95686274509803926, 0.792156862745098, 0.89411764705882357),
        (0.90196078431372551, 0.96078431372549022, 0.78823529411764703),
        (1.0, 0.94901960784313721, 0.68235294117647061),
        (0.94509803921568625, 0.88627450980392153, 0.8),
        (0.8, 0.8, 0.8)
    );

    public static DiscreteColorMap Set1 { get; } = Uniform(
        (0.89411764705882357, 0.10196078431372549, 0.10980392156862745),
        (0.21568627450980393, 0.49411764705882355, 0.72156862745098038),
        (0.30196078431372547, 0.68627450980392157, 0.29019607843137257),
        (0.59607843137254901, 0.30588235294117649, 0.63921568627450975),
        (1.0, 0.49803921568627452, 0.0),
        (1.0, 1.0, 0.2),
        (0.65098039215686276, 0.33725490196078434, 0.15686274509803921),
        (0.96862745098039216, 0.50588235294117645, 0.74901960784313726),
        (0.6, 0.6, 0.6)
    );

    public static DiscreteColorMap Set2 { get; } = Uniform(
        (0.4, 0.76078431372549016, 0.6470588235294118),
        (0.9882352941176471, 0.55294117647058827, 0.3843137254901961),
        (0.55294117647058827, 0.62745098039215685, 0.79607843137254897),
        (0.90588235294117647, 0.54117647058823526, 0.76470588235294112),
        (0.65098039215686276, 0.84705882352941175, 0.32941176470588235),
        (1.0, 0.85098039215686272, 0.18431372549019609),
        (0.89803921568627454, 0.7686274509803922, 0.58039215686274515),
        (0.70196078431372544, 0.70196078431372544, 0.70196078431372544)
    );

    public static DiscreteColorMap Set3 { get; } = Uniform(
        (0.55294117647058827, 0.82745098039215681, 0.7803921568627451),
        (1.0, 1.0, 0.70196078431372544),
        (0.74509803921568629, 0.72941176470588232, 0.85490196078431369),
        (0.98431372549019602, 0.50196078431372548, 0.44705882352941179),
        (0.50196078431372548, 0.69411764705882351, 0.82745098039215681),
        (0.99215686274509807, 0.70588235294117652, 0.3843137254901961),
        (0.70196078431372544, 0.87058823529411766, 0.41176470588235292),
        (0.9882352941176471, 0.80392156862745101, 0.89803921568627454),
        (0.85098039215686272, 0.85098039215686272, 0.85098039215686272),
        (0.73725490196078436, 0.50196078431372548, 0.74117647058823533),
        (0.8, 0.92156862745098034, 0.77254901960784317),
        (1.0, 0.92941176470588238, 0.43529411764705883)
    );

    public static DiscreteColorMap Jet { get; } = Uniform(
        0xff00008fu,
        0xff00009fu,
        0xff0000afu,
        0xff0000bfu,
        0xff0000cfu,
        0xff0000dfu,
        0xff0000efu,
        0xff0000ffu,
        0xff000fffu,
        0xff001fffu,
        0xff002fffu,
        0xff003fffu,
        0xff004fffu,
        0xff005fffu,
        0xff006fffu,
        0xff007fffu,
        0xff008fffu,
        0xff009fffu,
        0xff00afffu,
        0xff00bfffu,
        0xff00cfffu,
        0xff00dfffu,
        0xff00efffu,
        0xff00ffffu,
        0xff0fffefu,
        0xff1fffdfu,
        0xff2fffcfu,
        0xff3fffbfu,
        0xff4fffafu,
        0xff5fff9fu,
        0xff6fff8fu,
        0xff7fff7fu,
        0xff8fff6fu,
        0xff9fff5fu,
        0xffafff4fu,
        0xffbfff3fu,
        0xffcfff2fu,
        0xffdfff1fu,
        0xffefff0fu,
        0xffffff00u,
        0xffffef00u,
        0xffffdf00u,
        0xffffcf00u,
        0xffffbf00u,
        0xffffaf00u,
        0xffff9f00u,
        0xffff8f00u,
        0xffff7f00u,
        0xffff6f00u,
        0xffff5f00u,
        0xffff4f00u,
        0xffff3f00u,
        0xffff2f00u,
        0xffff1f00u,
        0xffff0f00u,
        0xffff0000u,
        0xffef0000u,
        0xffdf0000u,
        0xffcf0000u,
        0xffbf0000u,
        0xffaf0000u,
        0xff9f0000u,
        0xff8f0000u,
        0xff7f0000u
    );

    #endregion


    public RGBAColor this[Scalar c] => Interpolate(c);

    public RGBAColor this[Scalar c, Scalar min, Scalar max] => Interpolate(c, min, max);


    public abstract RGBAColor Interpolate(Scalar c);

    public RGBAColor Interpolate(Scalar c, Scalar min, Scalar max) => Interpolate((c - min) / (max - min));

    public static DiscreteColorMap Bicolor(RGBAColor c0, RGBAColor c1) => Uniform(c0, c1);

    public static DiscreteColorMap Tricolor(RGBAColor c0, RGBAColor c05, RGBAColor c1) => Uniform(c0, c05, c1);

    public static DiscreteColorMap Uniform(params RGBAColor[] colors) => new(colors);

    public static ContinuousColorMap Continuous(Func<Scalar, RGBAColor> function) => new(function);


    public static implicit operator ColorMap(RGBAColor[] colors) => Uniform(colors);

    public static implicit operator ColorMap(Func<Scalar, RGBAColor> function) => Continuous(function);
}

public class ContinuousColorMap
    : ColorMap
{
    private readonly Func<Scalar, RGBAColor> _func;


    public ContinuousColorMap(Func<Scalar, RGBAColor> function) => _func = function;

    public override RGBAColor Interpolate(Scalar c) => _func(c.Clamp());


    public static implicit operator ContinuousColorMap(Func<Scalar, RGBAColor> function) => Continuous(function);
}

public class DiscreteColorMap
    : ColorMap
{
    private readonly (Scalar X, RGBAColor Color)[] _colors;


    public DiscreteColorMap(params RGBAColor[] colors)
        : this(colors.Select((c, i) => (i / (Scalar)(colors.Length - 1), c)))
    {
    }

    public DiscreteColorMap(IEnumerable<RGBAColor> colors)
        : this(colors as RGBAColor[] ?? colors.ToArray())
    {
    }

    public DiscreteColorMap(params (Scalar X, RGBAColor Color)[] colors)
        : this(colors as IEnumerable<(Scalar, RGBAColor)>)
    {
    }

    public DiscreteColorMap(IEnumerable<(Scalar X, RGBAColor Color)> colors)
    {
        _colors = colors.OrderBy(c => c.X)
                        .Where(c => c.X >= 0 && c.X <= 1)
                        .ToArray();

        if (_colors.Length == 0)
            throw new ArgumentException("The color map must contain at least one color in the interval [0..1].", nameof(colors));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override RGBAColor Interpolate(Scalar c)
    {
        Scalar x0, x1;

        if (c <= _colors[0].X)
            return _colors[0].Color;
        else if (c >= _colors[^1].X)
            return _colors[^1].Color;

        for (int i = 1; i < _colors.Length; ++i)
        {
            x0 = _colors[i - 1].X;
            x1 = _colors[i].X;

            if (x0 <= c && c <= x1)
                return Vector4.LinearInterpolate(_colors[i - 1].Color, _colors[i].Color, (c - x0) / (x1 - x0));
        }

        throw new ArgumentOutOfRangeException(nameof(c));
    }

    public Scalar Approximate(RGBAColor color)
    {
        (Scalar x, Scalar fac)[] factors = (from pairs in _colors
                                            let dist = ((Vector3)pairs.Color).Subtract(color).SquaredLength
                                            let fac = (1 - dist).Clamp()
                                            select (pairs.X, fac)).ToArray();
        Scalar total = 0;
        Scalar sum = 0;

        for (int i = 0; i < factors.Length; ++i)
        {
            total += factors[i].fac;
            sum += factors[i].x * factors[i].fac;
        }

        return sum / total; // / factors.Length;
    }

    public ColorPalette ToColorPalette() => new(_colors.Select(LINQ.snd));


    public static implicit operator DiscreteColorMap(RGBAColor[] colors) => Uniform(colors);

    public static implicit operator ColorPalette(DiscreteColorMap map) => map.ToColorPalette();
}

public sealed record ColorTolerance(double Tolerance, ColorEqualityMetric Metric)
{
    public static ColorTolerance RGBADefault { get; } = new(Scalar.ComputationalEpsilon, ColorEqualityMetric.RGBAChannels);

    public static ColorTolerance RGBDefault { get; } = new(Scalar.ComputationalEpsilon, ColorEqualityMetric.RGBChannels);

    public static ColorTolerance None { get; } = new(0, ColorEqualityMetric.RGBAChannels);


    public ColorTolerance(double Tolerance)
        : this(Tolerance, ColorEqualityMetric.RGBAChannels)
    {
    }

    public double DistanceBetween<Color, Channel>(Color color1, Color color2)
        where Color : unmanaged, IColor<Color, Channel>
        where Channel : unmanaged => color1.DistanceTo(color2, Metric);

    public bool Equals<Color, Channel>(Color color1, Color color2)
        where Color : unmanaged, IColor<Color, Channel>
        where Channel : unmanaged => color1.Equals(color2, this);

    public static implicit operator ColorTolerance(double tolerance) => new(tolerance);

    public static implicit operator ColorEqualityMetric(ColorTolerance tolerance) => tolerance.Metric;

    public static implicit operator double(ColorTolerance tolerance) => tolerance.Tolerance;
}