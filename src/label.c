//
// ACME - a crossassembler for producing 6502/65c02/65816 code.
// Copyright (C) 1998-2006 Marco Baye
// Have a look at "acme.c" for further info
//
// Label stuff

#include <stdio.h>
#include <string.h>
#include "acme.h"
#include "alu.h"
#include "cpu.h"
#include "dynabuf.h"
#include "global.h"
#include "input.h"
#include "label.h"
#include "platform.h"
#include "section.h"
#include "tree.h"
#include "output.h"


// Constants
#define WARN_IF_LABEL_INDENTED	1	// FIXME - use CLI switch?


// Variables
node_ra_t*	Label_forest[256];// ... (because of 8-bit hash)

static char *getDeviceNameFromNumber(const int device)
{
	char *name = 0;
	switch(device)
	{
	default:
	case 0x00:
		name = "C";
		break;
	case 0x08:
		name = "8";
		break;
	case 0x40:
		name = "A";
		break;
	}
	return name;
}

// Dump label value and flags to dump file
static void dump_one_label(node_ra_t* node, FILE* fd) {
	label_t*	label	= node->body;

	// output name
	fprintf(fd, "%s", node->id_string);
	switch(label->result.flags & MVALUE_FORCEBITS) {

		case MVALUE_FORCE16:
		fprintf(fd, "+2=");
		break;

		case MVALUE_FORCE16 | MVALUE_FORCE24:
		/*FALLTHROUGH*/

		case MVALUE_FORCE24:
		fprintf(fd, "+3=");
		break;

		default:
		fprintf(fd, "  =");

	}
	if(label->result.flags & MVALUE_DEFINED) {
		if(label->result.flags & MVALUE_IS_FP)
			fprintf(fd, "%.30f", label->result.val.fpval);//FIXME %g
		else
			fprintf(fd, "$%x", (unsigned) label->result.val.intval);
	} else
		fprintf(fd, " ?");
	if(label->result.flags & MVALUE_UNSURE)
		fprintf(fd, "; ?");
	if(label->usage == 0)
		fprintf(fd, "; unused");
	if(label->device != 0)
		fprintf(fd, "; device=%s " , getDeviceNameFromNumber(label->device));
	fprintf(fd, "\n");
}

int gLabelCount = 0;

void CountLabelForPDB(node_ra_t* node, FILE* fd)
{
	label_t*	label	= node->body;
	if ( ! ( (label->result.flags & MVALUE_DEFINED) && !(label->result.flags & MVALUE_IS_FP) ) )
	{
		return;
	}
	gLabelCount++;
}

int gTheZone = 0;

void DumpLabelForPDB(node_ra_t* node, FILE* fd)
{
	label_t*	label	= node->body;
	if ( ! ( (label->result.flags & MVALUE_DEFINED) && !(label->result.flags & MVALUE_IS_FP) ) )
	{
		return;
	}

	fprintf( fd , "$%x:%d:%s" , label->result.val.intval , gTheZone , node->id_string );

	if ( label->usage )
	{
		fprintf(fd,":1");
	}
	else
	{
		fprintf(fd,":0");
	}

	if ( label->result.flags & MVALUE_IS_ADDRESS )
	{
		fprintf(fd,":1");
	}
	else
	{
		fprintf(fd,":0");
	}

	fprintf(fd,":%d\n" , label->device);
}

static void dump_one_label_vice(node_ra_t* node, FILE* fd)
{
//al C:09ae .nmi1

	label_t*	label	= node->body;

	// Dump labels that come from the PC even if they are not used
	if (//label->usage &&
		(label->result.flags & MVALUE_DEFINED) &&
		!(label->result.flags & MVALUE_IS_FP) &&
		(label->result.flags & MVALUE_IS_ADDRESS)
	)
	{
		fprintf(fd,"al %s:%0.4x .%s\n",getDeviceNameFromNumber(label->device),label->result.val.intval,node->id_string);
	}
}

static void dump_one_label_vice2(node_ra_t* node, FILE* fd)
{
//al C:09ae .nmi1

	label_t*	label	= node->body;

	// Dump other labels that are used
	if (label->usage &&
		(label->result.flags & MVALUE_DEFINED) &&
		!(label->result.flags & MVALUE_IS_FP) &&
		!(label->result.flags & MVALUE_IS_ADDRESS)
	)
	{
		fprintf(fd,"al %s:%0.4x .%s\n",getDeviceNameFromNumber(label->device),label->result.val.intval,node->id_string);
	}
}

static void dump_one_label_vice3(node_ra_t* node, FILE* fd)
{
//al C:09ae .nmi1

	label_t*	label	= node->body;

	// Dump other labels that are unused
	if (!label->usage &&
		(label->result.flags & MVALUE_DEFINED) &&
		!(label->result.flags & MVALUE_IS_FP) &&
		!(label->result.flags & MVALUE_IS_ADDRESS)
	)
	{
		fprintf(fd,"al C:%0.4x .%s\n",label->result.val.intval,node->id_string);
	}
}


// Search for label. Create if nonexistant. If created, give it flags "Flags".
// The label name must be held in GlobalDynaBuf.
label_t* Label_find(zone_t zone, int flags) {
	node_ra_t*	node;
	label_t*	label;
	bool		node_created;
	int		force_bits	= flags & MVALUE_FORCEBITS;

	node_created = Tree_hard_scan(&node, Label_forest, zone, TRUE);
	// if node has just been created, create label as well
	if(node_created) {
		// Create new label structure
		label = safe_malloc(sizeof(label_t));
		// Finish empty label item
		label->result.flags = flags;
		if(flags & MVALUE_IS_FP)
			label->result.val.fpval = 0;
		else
			label->result.val.intval = 0;
		label->usage = 0;// usage count
		label->pass = pass_count;
		label->device = current_device;
		node->body = label;
	} else
		label = node->body;
	// make sure the force bits don't clash
	if((node_created == FALSE) && force_bits)
		if((label->result.flags & MVALUE_FORCEBITS) != force_bits)
			Throw_error("Too late for postfix.");
	return(label);
}

static int valueToFind = 0;
static char theLabelHints[2048];

static void FindLabel(node_ra_t* node, FILE* fd)
{
	label_t*	label	= node->body;
	if ( ! ( (label->result.flags & MVALUE_DEFINED) && !(label->result.flags & MVALUE_IS_FP) ) )
	{
		return;
	}

	if (label->result.val.intval == valueToFind)
	{
		if (strlen(theLabelHints) < (sizeof(theLabelHints) - strlen(node->id_string) - 30))
		{
			strcat(theLabelHints , " : ");
			strcat(theLabelHints , node->id_string);
		}
	}
}

// Assign value to label. The function acts upon the label's flag bits and
// produces an error if needed.
int gLabel_set_value_changed_allowed = 0;
void Label_set_value(label_t* label, result_t* new, bool change_allowed) {
	int	oldflags	= label->result.flags;

	// This means the last pass with this mode enabled will not allow redefinition, and will flag any remaining errors
	if (gLabel_set_value_changed_allowed > 1) {
		change_allowed = TRUE;
	}

	// value stuff
	if((oldflags & MVALUE_DEFINED) && (change_allowed == FALSE)) {
		// Label is already defined, so compare new and old values
		// if different type OR same type but different value, complain
		if(((oldflags ^ new->flags) & MVALUE_IS_FP)
		|| ((oldflags & MVALUE_IS_FP)
		? (label->result.val.fpval != new->val.fpval)
		: (label->result.val.intval != new->val.intval)))
		{
			int i;
			valueToFind = label->result.val.intval;
			theLabelHints[0] = 0;
			strcat(theLabelHints , "Label already defined.");
			for (i=0;i<=zone_max;i++)
			{
				// Dump unused labels
				Tree_dump_forest(Label_forest, i, FindLabel, 0);
			}
			Throw_error(theLabelHints);
		}

	} else
		// Label is not defined yet OR redefinitions are allowed
		label->result = *new;
	// flags stuff
	// Ensure that "unsure" labels without "isByte" state don't get that
	if((oldflags & (MVALUE_UNSURE | MVALUE_ISBYTE)) == MVALUE_UNSURE)
		new->flags &= ~MVALUE_ISBYTE;
	if(change_allowed)
		oldflags = (oldflags & MVALUE_UNSURE) | new->flags;
	else {
		if((oldflags & MVALUE_FORCEBITS) == 0)
			if((oldflags & (MVALUE_UNSURE | MVALUE_DEFINED)) == 0)
				oldflags |= new->flags & MVALUE_FORCEBITS;
		oldflags |= new->flags & ~MVALUE_FORCEBITS;
	}
	label->result.flags = oldflags;
}

// (Re)set label
static enum eos_t PO_set(void) {// Now GotByte = illegal char
	result_t	result;
	int		force_bit;
	label_t*	label;
	zone_t		zone;

	if(Input_read_zone_and_keyword(&zone) == 0)	// skips spaces before
		// Now GotByte = illegal char
		return(SKIP_REMAINDER);
	force_bit = Input_get_force_bit();// skips spaces after
	label = Label_find(zone, force_bit);
	if(GotByte != '=') {
		Throw_error(exception_syntax);
		return(SKIP_REMAINDER);
	}
	// label = parsed value
	GetByte();// proceed with next char
	ALU_any_result(&result);
	// clear label's force bits and set new ones
	label->result.flags &= ~(MVALUE_FORCEBITS | MVALUE_ISBYTE);
	if(force_bit) {
		label->result.flags |= force_bit;
		result.flags &= ~(MVALUE_FORCEBITS | MVALUE_ISBYTE);
	}
	Label_set_value(label, &result, TRUE);
	return(ENSURE_EOS);
}

static enum eos_t PO_sal(void) {
	labeldump_allSections = 1;
	// ensure there's no garbage at end of line
	return(ENSURE_EOS);
}

static enum eos_t PO_disablesegmentcheck(void) {
	gPO_disablesegmentcheck = TRUE;
	return(ENSURE_EOS);
}
static enum eos_t PO_enablesegmentcheck(void) {
	gPO_disablesegmentcheck = FALSE;
	return(ENSURE_EOS);
}

static enum eos_t PO_previouscontext(void) {
	if (previouscontext_enable == 0)
	{
		previouscontext_enable++;
	}
	// ensure there's no garbage at end of line
	return(ENSURE_EOS);
}

static enum eos_t PO_labelredefine(void) {
	if (!pass_count)
	{
		gLabel_set_value_changed_allowed += 3;
	}
	// ensure there's no garbage at end of line
	return(ENSURE_EOS);
}

static enum eos_t PO_labelredefineend(void) {
	if (!pass_count)
	{
		if (gLabel_set_value_changed_allowed >= 4)
		{
			gLabel_set_value_changed_allowed -= 1;
		}
	}
	// ensure there's no garbage at end of line
	return(ENSURE_EOS);
}

// Select dump file
static enum eos_t PO_sl(void) {
	// only process this pseudo opcode in the first pass
	if(pass_count)
		return(SKIP_REMAINDER);
	// if label dump file already chosen, complain and exit
	if(labeldump_filename) {
		Throw_warning("Label dump file already chosen.");
		return(SKIP_REMAINDER);
	}
	// read filename to global dynamic buffer
	// if no file name given, exit (complaining will have been done)
	if(Input_read_filename(FALSE,FALSE))
		return(SKIP_REMAINDER);
	// get malloc'd copy of filename
	labeldump_filename = DynaBuf_get_copy(GlobalDynaBuf);
	// ensure there's no garbage at end of line
	return(ENSURE_EOS);
}

// Select dump file
static enum eos_t PO_svl(void) {
	// only process this pseudo opcode in the first pass
	if(pass_count)
		return(SKIP_REMAINDER);
	// if label dump file already chosen, complain and exit
	if(vicelabeldump_filename) {
		Throw_warning("Vice label dump file already chosen.");
		return(SKIP_REMAINDER);
	}
	// read filename to global dynamic buffer
	// if no file name given, exit (complaining will have been done)
	if(Input_read_filename(FALSE,FALSE))
		return(SKIP_REMAINDER);
	// get malloc'd copy of filename
	vicelabeldump_filename = DynaBuf_get_copy(GlobalDynaBuf);
	// ensure there's no garbage at end of line
	return(ENSURE_EOS);
}

static enum eos_t PO_pdb(void) {
	// only process this pseudo opcode in the first pass
	if(pass_count)
		return(SKIP_REMAINDER);
	// if label dump file already chosen, complain and exit
	if(PDB_filename) {
		Throw_warning("PDB file already chosen.");
		return(SKIP_REMAINDER);
	}
	// read filename to global dynamic buffer
	// if no file name given, exit (complaining will have been done)
	if(Input_read_filename(FALSE,FALSE))
		return(SKIP_REMAINDER);
	// get malloc'd copy of filename
	PDB_filename = DynaBuf_get_copy(GlobalDynaBuf);
	// ensure there's no garbage at end of line
	return(ENSURE_EOS);
}

// predefined stuff
static node_t	pseudo_opcodes[]	= {
	PREDEFNODE("set",	PO_set),
	PREDEFNODE("sal",	PO_sal),	// MPi: Added
	PREDEFNODE("sl",	PO_sl),
	PREDEFNODE("svl",	PO_svl),	// MPi: Added
	PREDEFNODE("pdb",	PO_pdb),		// MPi: Added
	PREDEFNODE("disablesegmentcheck",	PO_disablesegmentcheck),	// MPi: Added
	PREDEFNODE("enablesegmentcheck",	PO_enablesegmentcheck),	// MPi: Added
	PREDEFNODE("previouscontext",	PO_previouscontext),		// MPi: Added
	PREDEFNODE("labelredefine",	PO_labelredefine),		// MPi: Added
	PREDEFLAST("labelredefineend",	PO_labelredefineend)		// MPi: Added
	//    ^^^^ this marks the last element
};

// Parse implicit label definition (can be either global or local).
// GlobalDynaBuf holds the label name.
void Label_implicit_definition(zone_t zone, int stat_flags, int force_bit, bool change) {
	result_t	result;
	label_t*	label;

	label = Label_find(zone, force_bit);
	// implicit label definition (label)
	if((stat_flags & SF_FOUND_BLANK) && WARN_IF_LABEL_INDENTED)
		Throw_first_pass_warning("Implicit label definition not in leftmost column.");
	result.flags = CPU_pc.flags & (MVALUE_DEFINED | MVALUE_IS_ADDRESS);
	result.val.intval = CPU_pc.intval;
	Label_set_value(label, &result, change);
}

// Parse label definition (can be either global or local).
// GlobalDynaBuf holds the label name.
void Label_parse_definition(zone_t zone, int stat_flags) {
	result_t	result;
	label_t*	label;
	int		force_bit	= Input_get_force_bit();// skips spaces after
	// FIXME - force bit is allowed for implicit label defs?!

	if(GotByte == '=') {
		// explicit label definition (label = <something>)
		label = Label_find(zone, force_bit);
		// label = parsed value
		GetByte();	// skip '='
		ALU_any_result(&result);
		Label_set_value(label, &result, FALSE);
		Input_ensure_EOS();
	} else
		Label_implicit_definition(zone, stat_flags, force_bit, FALSE);
}

// Dump global labels to file
void Label_dump_all(FILE* fd) {
	Tree_dump_forest(Label_forest, ZONE_GLOBAL, dump_one_label, fd);
	if (labeldump_allSections)
	{
		zone_t i;
		for (i=1;i<=zone_max;i++)
		{
			Tree_dump_forest(Label_forest, i, dump_one_label, fd);
		}
	}
	PLATFORM_SETFILETYPE_TEXT(labeldump_filename);
}

void Label_dump_all_vice(FILE* fd) {

	zone_t theMax = 0;
	zone_t i;

	if (labeldump_allSections)
	{
		theMax = zone_max;
	}

	// The order of dumped labels is important because VICE will prefer later defined labels
	for (i=0;i<=theMax;i++)
	{
		// Dump unused labels
		Tree_dump_forest(Label_forest, i, dump_one_label_vice3, fd);
	}

	for (i=0;i<=theMax;i++)
	{
		// Dump other used labels
		Tree_dump_forest(Label_forest, i, dump_one_label_vice2, fd);
	}

	for (i=0;i<=theMax;i++)
	{
		// Dump labels that have MVALUE_IS_ADDRESS set
		Tree_dump_forest(Label_forest, i, dump_one_label_vice, fd);
	}

	PLATFORM_SETFILETYPE_TEXT(vicelabeldump_filename);
}

// register pseudo opcodes and clear label forest
void Label_init(void) {
	node_ra_t**	ptr;
	int		i;

	Tree_add_table(&pseudo_opcode_tree, pseudo_opcodes);
	// Cut down all the trees (clear pointer table)
	ptr = Label_forest;
	// Clear pointertable
	for(i = 255; i >= 0; i--)
		*ptr++ = NULL;
}

// Fix name of anonymous forward label (held in DynaBuf, NOT TERMINATED!) so it
// references the *next* anonymous forward label definition. The tricky bit is,
// each name length would need its own counter. But hey, ACME's real quick in
// finding labels, so I'll just abuse the label system to store those counters.
label_t* Label_fix_forward_name(void) {
	label_t*	counter_label;
	unsigned long	number;

	// terminate name, find "counter" label and read value
	DynaBuf_append(GlobalDynaBuf, '\0');
	counter_label = Label_find(Section_now->zone, 0);
	// make sure it gets reset to zero in each new pass
	if(counter_label->pass != pass_count) {
		counter_label->pass = pass_count;
		counter_label->result.val.intval = 0;
		counter_label->device = current_device;
	}
	number = (unsigned long) counter_label->result.val.intval;
	// now append to the name to make it unique
	GlobalDynaBuf->size--;	// forget terminator, we want to append
	do {
		DYNABUF_APPEND(GlobalDynaBuf, 'a' + (number & 15));
		number >>= 4;
	} while(number);
	DynaBuf_append(GlobalDynaBuf, '\0');
	return(counter_label);
}
