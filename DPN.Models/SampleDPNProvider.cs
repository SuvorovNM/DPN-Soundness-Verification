using DPN.Models.Abstractions;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using Microsoft.Z3;

namespace DPN.Models
{
	public class SampleDPNProvider
	{
		public DataPetriNet GetVOVDataPetriNet()
		{
			var context = new Context();
			var placesList = new List<Place>
			{
				new("i", PlaceType.Initial),
				new("p1", PlaceType.Intermediary),
				new("p2", PlaceType.Intermediary),
				new("p3", PlaceType.Intermediary),
				new("p4", PlaceType.Intermediary),
				new("p5", PlaceType.Intermediary),
				new("p6", PlaceType.Intermediary),
				new("p7", PlaceType.Intermediary),
				new("p8", PlaceType.Intermediary),
				new("p9", PlaceType.Intermediary),
				new("o", PlaceType.Final)
			};

			var variables = new VariablesStore();
			variables[DomainType.Real].Write("reqd", new DefinableValue<double>(0));
			variables[DomainType.Real].Write("granted", new DefinableValue<double>(0));
			variables[DomainType.Boolean].Write("ok", new DefinableValue<bool>(false));

			var transitionList = new List<Transition>
			{
				new("Credit request", new Guard(context,
					context.MkGt(context.MkRealConst("reqd_w"), context.MkReal(0)))),

				new("Verify", new Guard(context,
					context.MkOr(
						context.MkEq(context.MkBoolConst("ok_w"), context.MkBool(true)),
						context.MkEq(context.MkBoolConst("ok_w"), context.MkBool(false))
					))),

				new("Prepare", new Guard(context,
					context.MkEq(context.MkBoolConst("ok_r"), context.MkBool(true)))),

				new("Skip", new Guard(context,
					context.MkEq(context.MkBoolConst("ok_r"), context.MkBool(false)))),

				new("Make proposal", new Guard(context,
					context.MkLe(context.MkRealConst("granted_w"), context.MkRealConst("reqd_r")))),

				new("Refuse proposal", new Guard(context,
					context.MkNot(context.MkEq(context.MkRealConst("granted_r"), context.MkRealConst("reqd_r"))))),

				new("Update request", new Guard(context,
					context.MkLt(context.MkRealConst("reqd_w"), context.MkRealConst("reqd_r")))),

				new("AND split", new Guard(context, null)),

				new("Inform acceptance VIP", new Guard(context,
					context.MkGt(context.MkRealConst("granted_r"), context.MkReal(10000)))),

				new("Inform rejection", new Guard(context,
					context.MkEq(context.MkBoolConst("ok_r"), context.MkBool(false)))),

				new("Open credit loan", new Guard(context,
					context.MkEq(context.MkBoolConst("ok_r"), context.MkBool(true)))),

				new("AND join", new Guard(context, null)),
			};

			var arcsList = new List<Arc>
			{
				new(placesList[0], transitionList[0]),
				new(transitionList[0], placesList[1]),
				new(placesList[1], transitionList[1]),
				new(transitionList[1], placesList[2]),
				new(placesList[2], transitionList[2]),
				new(placesList[2], transitionList[3]),
				new(transitionList[3], placesList[3]),
				new(transitionList[2], placesList[8]),
				new(placesList[8], transitionList[4]),
				new(transitionList[4], placesList[3]),
				new(placesList[3], transitionList[5]),
				new(transitionList[5], placesList[9]),
				new(placesList[9], transitionList[6]),
				new(transitionList[6], placesList[1]),
				new(placesList[3], transitionList[7]),
				new(transitionList[7], placesList[4]),
				new(transitionList[7], placesList[5]),
				new(placesList[4], transitionList[8]),
				new(placesList[4], transitionList[9]),
				new(transitionList[8], placesList[6]),
				new(transitionList[9], placesList[6]),
				new(placesList[5], transitionList[10]),
				new(transitionList[10], placesList[7]),
				new(placesList[6], transitionList[11]),
				new(placesList[7], transitionList[11]),
				new(transitionList[11], placesList[10]),
			};

			return new DataPetriNet(context)
			{
				Name = "VOV model",
				Id = "net_vov",
				Places = placesList,
				Transitions = transitionList,
				Variables = variables,
				Arcs = arcsList
			};
		}

		public DataPetriNet GetVOCDataPetriNet()
		{
			var context = new Context();
			var placesList = new List<Place>
			{
				new("i", PlaceType.Initial),
				new("p1", PlaceType.Intermediary),
				new("p2", PlaceType.Intermediary),
				new("p3", PlaceType.Intermediary),
				new("p4", PlaceType.Intermediary),
				new("p5", PlaceType.Intermediary),
				new("p6", PlaceType.Intermediary),
				new("p7", PlaceType.Intermediary),
				new("o", PlaceType.Final)
			};

			var variables = new VariablesStore();
			variables[DomainType.Real].Write("amount", new DefinableValue<double>(0));
			variables[DomainType.Boolean].Write("ok", new DefinableValue<bool>(false));

			var transitionList = new List<Transition>
			{
				new("Credit request", new Guard(context,
					context.MkGe(context.MkRealConst("amount_w"), context.MkReal(0)))),

				new("Verify", new Guard(context,
					context.MkOr(
						context.MkEq(context.MkBoolConst("ok_w"), context.MkBool(true)),
						context.MkEq(context.MkBoolConst("ok_w"), context.MkBool(false))
					))),

				new("Skip assessment", new Guard(context,
					context.MkEq(context.MkBoolConst("ok_r"), context.MkBool(false)))),

				new("Simple assessment", new Guard(context,
					context.MkAnd(
						context.MkEq(context.MkBoolConst("ok_r"), context.MkBool(true)),
						context.MkEq(context.MkBoolConst("ok_w"), context.MkBool(true)),
						context.MkLt(context.MkRealConst("amount_r"), context.MkReal(5000))
					))),

				new("Advanced assessment", new Guard(context,
					context.MkAnd(
						context.MkEq(context.MkBoolConst("ok_r"), context.MkBool(true)),
						context.MkEq(context.MkBoolConst("ok_w"), context.MkBool(true)),
						context.MkGe(context.MkRealConst("amount_r"), context.MkReal(5000))
					))),

				new("Renegotiate request", new Guard(context,
					context.MkAnd(
						context.MkGe(context.MkRealConst("amount_r"), context.MkReal(15000)),
						context.MkEq(context.MkBoolConst("ok_r"), context.MkBool(false))
					))),

				new("AND split", new Guard(context, null)),

				new("Inform acceptance customer normal", new Guard(context,
					context.MkAnd(
						context.MkEq(context.MkBoolConst("ok_r"), context.MkBool(true)),
						context.MkLt(context.MkRealConst("amount_r"), context.MkReal(10000))
					))),

				new("Inform acceptance customer VIP", new Guard(context,
					context.MkAnd(
						context.MkEq(context.MkBoolConst("ok_r"), context.MkBool(true)),
						context.MkGe(context.MkRealConst("amount_r"), context.MkReal(10000))
					))),

				new("Inform rejection customer VIP", new Guard(context,
					context.MkAnd(
						context.MkEq(context.MkBoolConst("ok_r"), context.MkBool(false)),
						context.MkGe(context.MkRealConst("amount_r"), context.MkReal(10000))
					))),

				new("Open credit loan", new Guard(context,
					context.MkEq(context.MkBoolConst("ok_r"), context.MkBool(true)))),

				new("AND join", new Guard(context, null)),
			};

			var arcsList = new List<Arc>
			{
				new(placesList[0], transitionList[0]),
				new(transitionList[0], placesList[1]),
				new(placesList[1], transitionList[1]),
				new(transitionList[1], placesList[2]),
				new(placesList[2], transitionList[2]),
				new(placesList[2], transitionList[3]),
				new(placesList[2], transitionList[4]),
				new(transitionList[2], placesList[3]),
				new(transitionList[3], placesList[3]),
				new(transitionList[4], placesList[3]),
				new(placesList[3], transitionList[5]),
				new(transitionList[5], placesList[1]),
				new(placesList[3], transitionList[6]),
				new(transitionList[6], placesList[4]),
				new(transitionList[6], placesList[5]),
				new(placesList[4], transitionList[7]),
				new(placesList[4], transitionList[8]),
				new(placesList[4], transitionList[9]),
				new(transitionList[7], placesList[6]),
				new(transitionList[8], placesList[6]),
				new(transitionList[9], placesList[6]),
				new(placesList[5], transitionList[10]),
				new(transitionList[10], placesList[7]),
				new(placesList[6], transitionList[11]),
				new(placesList[7], transitionList[11]),
				new(transitionList[11], placesList[8]),
			};

			return new DataPetriNet(context)
			{
				Name = "VOC model",
				Id = "net_voc",
				Places = placesList,
				Transitions = transitionList,
				Variables = variables,
				Arcs = arcsList
			};
		}
	}
}