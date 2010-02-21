var x=0;

function printHidden() {
if (window.navigator.userAgent.indexOf("MSIE ")!=-1 && navigator.appVersion.substr(0, 1) >= 4){
  document.body.insertAdjacentHTML("beforeEnd", 
    "<iframe name='printHiddenFrame' width='0' height='0'></iframe>");
  doc = printHiddenFrame.document;
  doc.open();
  doc.write(
    "<frameset onload='parent.printFrame(printMe);' rows=\"100%\">" +
    "<frame name=printMe src=\"print.asp\">" +
    "</frameset>");
  doc.close();
}
else{
  document.location.href="print.asp";
}
}

function printFrame(frame) {
  frame.focus();
  frame.print();
  return;
}


function OpenUrl(url)
{
var childWin;
childWin = window.open(url,'window','height=300,width=550,status=yes,toolbar=no,menubar=no,location=no,scrollbars=yes,resizable=yes');
childWin.focus();
}

function ToggleDisplay(oButton, oItems)
{
	if (oItems.style.display == "none")	{
		oItems.style.display = "";
		oButton.src = "/msdnmag/images/minus.gif";
	}	else {
		oItems.style.display = "none";
		oButton.src = "/msdnmag/images/plus.gif";
	}
	return;
}
function change()	{
	var coll = document.all.tags("DIV");
	if (x!=1){
	{for (i=0; i<coll.length; i++)
		if (coll[i].style.display=='none' && coll[i].id.indexOf("menu")>-1){
			coll[i].style.display='';
		}
	}
	button1.value=" Collapse All "
	x=1
	var coll2 = document.all.tags("IMG");
	{for (i=0; i<coll2.length; i++)
		if (coll2[i].id.indexOf("btns")>-1){
			coll2[i].src='/msdnmag/images/minus.gif';
		}
	}

	}
	else {
	{for (i=0; i<coll.length; i++)
		if (coll[i].style.display=='' && coll[i].id.indexOf("menu")>-1){
			coll[i].style.display='none';
		}
	}
	button1.value=" Expand All "
	x=0
	var coll2 = document.all.tags("IMG");
	{for (i=0; i<coll2.length; i++)
		if (coll2[i].id.indexOf("btns")>-1){
			coll2[i].src='/msdnmag/images/plus.gif';
		}
	}
	}
}
